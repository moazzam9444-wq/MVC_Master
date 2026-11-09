using KicksMvc.Models.Entities;
using KicksMvc.Services.Activities;
using KicksMvc.ViewModels;
using Microsoft.EntityFrameworkCore;
namespace KicksMvc.Services.Tournaments;

public sealed partial class TournamentService
{
    sealed class Record
    {
        public int RegistrationId {get;set;}public string Team {get;set;}="";public int Played {get;set;}public int Wins {get;set;}public int Draws {get;set;}public int Losses {get;set;}public int GoalsFor {get;set;}public int GoalsAgainst {get;set;}public int GoalDifference=>GoalsFor-GoalsAgainst;public int Points=>Wins*3+Draws;
    }
    string State(Tournament t,int count,bool completed,bool started)=>t.Status=="Cancelled"?"Cancelled":completed?"Completed":t.DrawStartedAt!=null?(started?"In Progress":"Scheduled"):t.MatchDurationMinutes==null?"Legacy history":rules.Now<t.RegistrationOpensAt?"Registration Not Open":rules.Now>=t.RegistrationClosesAt||rules.Now>=t.StartsAt?"Registration Closed":count>=t.Capacity?"Registration Full":"Registration Open";
    public async Task<object> Details(int actor,int id,bool ownerRole,bool playerRole)
    {
        var t=await db.Tournaments.AsNoTracking().Include(t=>t.Field).ThenInclude(f=>f.Owner).SingleOrDefaultAsync(t=>t.Id==id)??throw new ActivityException("Tournament not found.",404);
        var owner=ownerRole&&t.CreatedBy==actor&&t.Field.OwnerId==actor;var eligible=await fields.PublicFields.AnyAsync(f=>f.Id==t.FieldId);
        var currentTeam=await db.TeamMembers.AsNoTracking().Where(m=>m.UserId==actor).Select(m=>(int?)m.TeamId).SingleOrDefaultAsync();var captain=await db.Teams.AsNoTracking().Where(team=>playerRole&&team.CaptainId==actor).Select(team=>new{team.Id,team.Name,team.Format}).SingleOrDefaultAsync();
        var involved=actor>0&&await db.TournamentRegistrations.AnyAsync(r=>r.TournamentId==id&&(r.TeamId==currentTeam||r.Players.Any(p=>p.UserId==actor)));
        if(!eligible&&!owner&&!involved)throw new ActivityException("Tournament not found.",404);
        var entries=await db.TournamentRegistrations.AsNoTracking().Where(r=>r.TournamentId==id).OrderBy(r=>r.Id).Take(64).Select(r=>new{r.Id,r.TeamId,Team=r.TeamNameSnapshot,r.Status,RosterCount=r.Players.Count,IsMine=r.TeamId==currentTeam,IsCaptainEntry=captain!=null&&r.TeamId==captain.Id}).ToListAsync();
        var activeCount=await db.TournamentRegistrations.CountAsync(r=>r.TournamentId==id&&r.Status=="Registered");
        var matches=await db.TournamentMatches.AsNoTracking().Where(m=>m.TournamentId==id).Include(m=>m.InputSlots).ThenInclude(s=>s.SourceMatch).OrderBy(m=>m.RoundNumber).ThenBy(m=>m.MatchNumber).Take(63).ToListAsync();
        var final=matches.LastOrDefault();var champion=final?.Status=="Completed"&&final.WinnerRegistrationId!=null?entries.FirstOrDefault(r=>r.Id==final.WinnerRegistrationId):null;
        var mine=entries.FirstOrDefault(r=>r.IsCaptainEntry)??entries.FirstOrDefault(r=>r.IsMine);var open=RegistrationOpen(t)&&eligible;var modern=t.MatchDurationMinutes!=null;var manage=owner&&modern&&t.Status!="Cancelled"&&champion==null;
        var table=entries.Where(r=>r.Status=="Registered").ToDictionary(r=>r.Id,r=>new Record{RegistrationId=r.Id,Team=r.Team});
        foreach(var m in matches.Where(m=>m.Status=="Completed"&&!m.IsBye))
        {
            var slots=m.InputSlots.OrderBy(s=>s.SlotNumber).ToArray();if(slots.Length!=2||slots.Any(s=>s.RegistrationId==null)||m.HomeScore is null||m.AwayScore is null||!table.TryGetValue(slots[0].RegistrationId!.Value,out var home)||!table.TryGetValue(slots[1].RegistrationId!.Value,out var away))continue;
            home.Played++;away.Played++;home.GoalsFor+=m.HomeScore.Value;home.GoalsAgainst+=m.AwayScore.Value;away.GoalsFor+=m.AwayScore.Value;away.GoalsAgainst+=m.HomeScore.Value;
            if(m.WinnerRegistrationId==home.RegistrationId){home.Wins++;away.Losses++;}else{away.Wins++;home.Losses++;}
        }
        var standings=table.Values.OrderByDescending(r=>r.RegistrationId==champion?.Id).ThenByDescending(r=>r.Wins).ThenByDescending(r=>r.GoalDifference).ThenByDescending(r=>r.GoalsFor).ThenBy(r=>r.RegistrationId).ToList();
        var fixtures=matches.Select(m=>new{m.Id,m.RoundNumber,m.MatchNumber,m.IsBye,m.Status,m.StartsAt,m.EndsAt,m.DurationMinutes,m.HomeScore,m.AwayScore,m.HomePenalties,m.AwayPenalties,m.WinnerRegistrationId,m.UpdatedAt,UpdatedBy=owner?m.UpdatedBy:null,Version=owner?MatchVersion(m):null,Slots=m.InputSlots.OrderBy(s=>s.SlotNumber).Select(s=>new{s.SlotNumber,s.RegistrationId,s.SourceMatchId,Team=entries.FirstOrDefault(r=>r.Id==s.RegistrationId)?.Team}),CanSchedule=manage&&t.EndsAt>rules.Now&&!m.IsBye&&m.Status!="Completed"&&(m.Status!="Scheduled"||m.StartsAt>rules.Now)&&m.InputSlots.Count==2&&m.InputSlots.All(s=>s.RegistrationId!=null&&(s.SourceMatchId==null||s.SourceMatch?.Status=="Completed")),CanCancel=manage&&!m.IsBye&&(m.Status=="Draft"||m.Status=="Scheduled"&&m.StartsAt>rules.Now),CanResult=manage&&m.Status=="Scheduled"&&m.EndsAt<=rules.Now&&!m.IsBye}).ToList();
        var image=await db.FieldImages.Where(i=>i.FieldId==t.FieldId).OrderBy(i=>i.SortOrder).Select(i=>i.StorageUrl).FirstOrDefaultAsync();
        return new{t.Id,t.Name,t.Description,t.FieldId,Field=t.Field.Name,t.Field.City,t.Field.Format,Organizer=t.Field.Owner.FullName,Image=image==null?"/assets/images/field3.jpg":"/media/fields/"+Uri.EscapeDataString(image),t.StartsAt,t.EndsAt,t.RegistrationOpensAt,t.RegistrationClosesAt,t.MatchDurationMinutes,t.Capacity,t.EntryFee,t.Prize,t.System,t.Level,t.DrawStartedAt,Count=activeCount,Status=State(t,activeCount,champion!=null,matches.Any(m=>m.StartsAt<=rules.Now)),Champion=champion==null?null:new{champion.Id,champion.Team},Standings=standings,Matches=fixtures,Entries=entries,OwnTeam=captain,OwnRegistration=mine,IsOwner=owner,Legacy=!modern,CanManage=manage,CoreLocked=t.DrawStartedAt!=null||entries.Count>0,CanRegister=modern&&open&&captain!=null&&captain.Format==t.Field.Format&&activeCount<t.Capacity&&mine?.Status!="Registered",CanWithdraw=RegistrationOpen(t)&&captain!=null&&mine?.IsCaptainEntry==true&&mine.Status=="Registered",CanGenerate=manage&&t.DrawStartedAt==null&&rules.Now<t.StartsAt&&activeCount>=2&&(rules.Now>=t.RegistrationClosesAt||activeCount==t.Capacity),Version=owner?await Version(t):null,Payment=t.EntryFee>0?"Payment Not Processed":"Free entry",NoPayment=true};
    }
    public async Task<object> List(int actor,bool owner,TournamentListInput input)
    {
        var q=db.Tournaments.AsNoTracking();
        if(input.Mode=="mine"){if(!owner)throw new ActivityException("Owner access required.",403);q=q.Where(t=>t.CreatedBy==actor&&t.Field.OwnerId==actor);}
        else if(input.Mode=="registered")q=q.Where(t=>db.TournamentRegistrations.Any(r=>r.TournamentId==t.Id&&(r.Team.Members.Any(m=>m.UserId==actor)||r.Players.Any(p=>p.UserId==actor))));
        else q=q.Where(t=>t.Status!="Cancelled"&&fields.PublicFields.Any(f=>f.Id==t.FieldId));
        if(!string.IsNullOrWhiteSpace(input.Search)){var s=input.Search.Trim();q=q.Where(t=>t.Name.Contains(s)||t.Field.Name.Contains(s)||t.Field.City.Contains(s));}
        var total=await q.CountAsync();var page=Math.Min(input.Page,Math.Max(1,(int)Math.Ceiling(total/12d)));
        var rows=await q.OrderByDescending(t=>t.StartsAt).ThenBy(t=>t.Id).Skip((page-1)*12).Take(12).Select(t=>new{t.Id,t.Name,t.Description,t.FieldId,Field=t.Field.Name,t.Field.City,t.Field.Format,t.StartsAt,t.EndsAt,t.MatchDurationMinutes,t.Capacity,t.EntryFee,t.Prize,t.System,t.Status,t.DrawStartedAt,t.RegistrationOpensAt,t.RegistrationClosesAt,Count=t.Registrations.Count(r=>r.Status=="Registered"),Completed=t.Matches.Any(m=>m.Status=="Completed"&&m.WinnerRegistrationId!=null&&m.RoundNumber==t.Matches.Max(x=>x.RoundNumber)),Started=t.Matches.Any(m=>m.StartsAt<=rules.Now),Image=t.Field.Images.OrderBy(i=>i.SortOrder).Select(i=>"/media/fields/"+i.StorageUrl).FirstOrDefault()??"/assets/images/field3.jpg"}).ToListAsync();
        return new{Items=rows.Select(r=>new{r.Id,r.Name,r.Description,r.FieldId,r.Field,r.City,r.Format,r.StartsAt,r.EndsAt,r.MatchDurationMinutes,r.Capacity,r.EntryFee,r.Prize,r.System,r.Count,r.Image,Status=State(new Tournament{Status=r.Status,MatchDurationMinutes=r.MatchDurationMinutes,DrawStartedAt=r.DrawStartedAt,RegistrationOpensAt=r.RegistrationOpensAt,RegistrationClosesAt=r.RegistrationClosesAt,StartsAt=r.StartsAt,Capacity=r.Capacity},r.Count,r.Completed,r.Started)}),Total=total,Page=page,PageSize=12};
    }
    public async Task<object> Summary(int actor,bool owner,bool admin)
    {
        var q=db.Tournaments.AsNoTracking().Where(t=>t.Status!="Cancelled"&&t.EndsAt>rules.Now&&!t.Matches.Any(m=>m.Status=="Completed"&&m.WinnerRegistrationId!=null&&m.RoundNumber==t.Matches.Max(x=>x.RoundNumber)));
        if(owner)q=q.Where(t=>t.CreatedBy==actor&&t.Field.OwnerId==actor);else if(!admin)q=q.Where(t=>t.Registrations.Any(r=>r.Status=="Registered"&&(r.Team.Members.Any(m=>m.UserId==actor)||r.Players.Any(p=>p.UserId==actor))));else q=q.Where(t=>fields.PublicFields.Any(f=>f.Id==t.FieldId));
        return new{Total=await q.CountAsync(),Entries=await db.TournamentRegistrations.CountAsync(r=>r.Status=="Registered"&&q.Any(t=>t.Id==r.TournamentId)),ByField=await q.GroupBy(t=>t.FieldId).Select(g=>new{FieldId=g.Key,Count=g.Count()}).ToListAsync(),Items=await q.OrderBy(t=>t.StartsAt).Take(12).Select(t=>new{t.Id,t.Name,t.FieldId,t.StartsAt,t.DurationMinutes}).ToListAsync()};
    }
}
