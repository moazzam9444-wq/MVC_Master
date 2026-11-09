using KicksMvc.Models.Entities;
using KicksMvc.Services.Activities;
using KicksMvc.Services.Bookings;
using KicksMvc.ViewModels;
using Microsoft.EntityFrameworkCore;
namespace KicksMvc.Services.Tournaments;

public sealed partial class TournamentService
{
    async Task<TournamentMatche> Match(int tournamentId,int matchId)=>await db.TournamentMatches.Include(m=>m.InputSlots).ThenInclude(s=>s.SourceMatch).SingleOrDefaultAsync(m=>m.Id==matchId&&m.TournamentId==tournamentId)??throw new ActivityException("Match not found in this tournament.",404);
    string MatchVersion(TournamentMatche m)=>rules.Version(new{Kind="TournamentMatch",m.Id,m.TournamentId,m.Status,m.StartsAt,m.DurationMinutes,m.HomeScore,m.AwayScore,m.HomePenalties,m.AwayPenalties,m.WinnerRegistrationId,m.IsBye,m.UpdatedAt,Slots=m.InputSlots.OrderBy(s=>s.SlotNumber).Select(s=>new{s.SlotNumber,s.RegistrationId,s.SourceMatchId,SourceWinner=s.SourceMatch?.WinnerRegistrationId,SourceStatus=s.SourceMatch?.Status})});
    static int[] Participants(TournamentMatche m)
    {
        var slots=m.InputSlots.OrderBy(s=>s.SlotNumber).ToArray();if(slots.Length!=2||slots.Any(s=>s.RegistrationId is null||s.SourceMatchId!=null&&s.SourceMatch?.Status!="Completed")||slots[0].RegistrationId==slots[1].RegistrationId)throw new ActivityException("Both teams must be resolved by completed earlier rounds before scheduling or playing this match.");return slots.Select(s=>s.RegistrationId!.Value).ToArray();
    }
    public async Task Schedule(int actor,int tournamentId,int matchId,TournamentScheduleInput input)
    {
        var start=rules.Start(input);var end=start.AddMinutes(input.DurationMinutes);
        var teamIds=await db.TournamentMatchSlots.AsNoTracking().Where(s=>s.TournamentId==tournamentId&&s.MatchId==matchId&&s.RegistrationId!=null).Select(s=>s.Registration!.TeamId).Distinct().ToListAsync();
        await using var tx=await db.Database.BeginTransactionAsync();await LockTeams(teamIds);var(t,field)=await Gate(tournamentId);Owner(t,field,actor);await Editable(t);await Eligible(field.Id);var m=await Match(tournamentId,matchId);rules.Check(input.Version,MatchVersion(m));var players=Participants(m);
        if(t.DrawStartedAt==null||m.IsBye||m.Status=="Completed"||m.Status=="Scheduled"&&m.StartsAt<=rules.Now||input.DurationMinutes!=t.MatchDurationMinutes||start<t.StartsAt||end>t.EndsAt)throw new ActivityException("Choose the tournament's match duration and an unplayed future interval within its event window.");
        var currentTeams=await db.TournamentRegistrations.Where(r=>players.Contains(r.Id)&&r.TournamentId==tournamentId&&r.Status=="Registered").Select(r=>r.TeamId).ToListAsync();if(currentTeams.Count!=2||currentTeams.Any(id=>!teamIds.Contains(id)))throw new ActivityException("Fixture teams changed. Refresh.");
        foreach(var s in m.InputSlots)if(s.SourceMatch?.EndsAt is DateTimeOffset previousEnd&&start<previousEnd)throw new ActivityException("This match must start after its feeder matches end.");
        if(await FieldOccupancy.Occupied(db,field.Id,start,end,excludeTournamentMatch:matchId))throw new ActivityException("This field interval is already reserved by a booking, game or tournament match.");
        if(await db.TournamentMatchSlots.AnyAsync(s=>s.MatchId!=matchId&&s.RegistrationId!=null&&currentTeams.Contains(s.Registration!.TeamId)&&(s.Match.Status=="Scheduled"||s.Match.Status=="Completed")&&s.Match.StartsAt<end&&s.Match.EndsAt>start))throw new ActivityException("A participating team has another tournament match at this time.");
        m.StartsAt=start;m.DurationMinutes=input.DurationMinutes;m.Status="Scheduled";m.UpdatedBy=actor;m.UpdatedAt=rules.Now;await db.SaveChangesAsync();await tx.CommitAsync();
    }
    public async Task CancelMatch(int actor,int tournamentId,int matchId,string version)
    {
        await using var tx=await db.Database.BeginTransactionAsync();var(t,field)=await Gate(tournamentId);Owner(t,field,actor);await Editable(t);var m=await Match(tournamentId,matchId);rules.Check(version,MatchVersion(m));
        if(m.IsBye||m.Status=="Completed"||m.Status=="Cancelled"||m.Status=="Scheduled"&&m.StartsAt<=rules.Now)throw new ActivityException("Only an unplayed draft or future match can be cancelled.");
        m.Status="Cancelled";m.UpdatedBy=actor;m.UpdatedAt=rules.Now;await db.SaveChangesAsync();await tx.CommitAsync();
    }
    public async Task Result(int actor,int tournamentId,int matchId,TournamentResultInput input)
    {
        await using var tx=await db.Database.BeginTransactionAsync();var(t,field)=await Gate(tournamentId);Owner(t,field,actor);await Editable(t);var m=await Match(tournamentId,matchId);rules.Check(input.Version,MatchVersion(m));var entries=Participants(m);
        if(m.Status!="Scheduled"||m.IsBye||m.EndsAt is null||m.EndsAt>rules.Now)throw new ActivityException("Finalize only an ended scheduled match. Completed results are immutable to protect advancement.");
        if(input.HomeScore is null||input.AwayScore is null||input.HomeScore is <0 or >99||input.AwayScore is <0 or >99)throw new ActivityException("Enter scores from 0 to 99.",400);
        var tied=input.HomeScore==input.AwayScore;if(tied?(input.HomePenalties is null||input.AwayPenalties is null||input.HomePenalties==input.AwayPenalties):(input.HomePenalties!=null||input.AwayPenalties!=null))throw new ActivityException("A tied knockout score requires unequal penalty shootout scores. Otherwise leave penalties empty.",400);
        if(input.HomePenalties is <0 or >99||input.AwayPenalties is <0 or >99)throw new ActivityException("Penalty scores must be from 0 to 99.",400);
        var winner=tied?input.HomePenalties>input.AwayPenalties:input.HomeScore>input.AwayScore;m.WinnerRegistrationId=entries[winner?0:1];m.HomeScore=input.HomeScore;m.AwayScore=input.AwayScore;m.HomePenalties=input.HomePenalties;m.AwayPenalties=input.AwayPenalties;m.Status="Completed";m.UpdatedBy=actor;m.UpdatedAt=rules.Now;
        var outputs=await db.TournamentMatchSlots.Where(s=>s.TournamentId==tournamentId&&s.SourceMatchId==matchId).Include(s=>s.Match).ToListAsync();
        if(outputs.Count>1||outputs.Any(s=>s.Match.Status is "Scheduled" or "Completed"))throw new ActivityException("A later match is already committed; earlier results cannot change.");
        foreach(var s in outputs)s.RegistrationId=m.WinnerRegistrationId;await ResolveByes(actor,tournamentId);await db.SaveChangesAsync();await tx.CommitAsync();
    }
}
