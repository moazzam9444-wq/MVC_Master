using KicksMvc.Models.Entities;
using KicksMvc.Services.Activities;
using Microsoft.EntityFrameworkCore;
namespace KicksMvc.Services.Tournaments;

public sealed partial class TournamentService
{
    async Task<List<Team>> LockTeams(IEnumerable<int> ids)
    {
        var result=new List<Team>();foreach(var id in ids.Distinct().Order())result.Add(await db.Teams.FromSqlInterpolated($"SELECT * FROM dbo.teams WITH(UPDLOCK,HOLDLOCK) WHERE id={id}").SingleOrDefaultAsync()??throw new ActivityException("Team not found.",404));return result;
    }
    async Task<Team> CaptainTeam(int actor)
    {
        var id=await db.Teams.AsNoTracking().Where(t=>t.CaptainId==actor).Select(t=>(int?)t.Id).SingleOrDefaultAsync()??throw new ActivityException("Only a current team captain may register or withdraw a team.",403);
        var team=(await LockTeams([id])).Single();if(team.CaptainId!=actor)throw new ActivityException("Captaincy changed. Refresh this tournament.",403);await ValidTeam(team);return team;
    }
    async Task ValidTeam(Team team)
    {
        if(!await db.TeamMembers.AnyAsync(m=>m.TeamId==team.Id&&m.UserId==team.CaptainId)||!await db.Users.AnyAsync(u=>u.Id==team.CaptainId&&u.Status=="Active")||!await db.UserRoles.AnyAsync(ur=>ur.UserId==team.CaptainId&&db.Roles.Any(r=>r.Id==ur.RoleId&&r.Name=="player")))throw new ActivityException("The team must have an Active player captain and a valid roster.");
    }
    bool RegistrationOpen(Tournament t)=>t.Status=="Scheduled"&&t.MatchDurationMinutes!=null&&t.DrawStartedAt==null&&t.RegistrationOpensAt<=rules.Now&&rules.Now<t.RegistrationClosesAt&&rules.Now<t.StartsAt;
    public async Task<bool> Register(int actor,int id)
    {
        await using var tx=await db.Database.BeginTransactionAsync();var team=await CaptainTeam(actor);var(t,field)=await Gate(id);Modern(t);await Eligible(field.Id);
        var existing=await db.TournamentRegistrations.SingleOrDefaultAsync(r=>r.TournamentId==id&&r.TeamId==team.Id);
        if(existing?.Status=="Registered"){await tx.CommitAsync();return true;}
        if(existing?.Status=="Pending"&&existing.PaymentDeadline>rules.Now){await tx.CommitAsync();return true;}
        if(existing is not null)await payments.Cancel(registrationId:existing.Id);
        if(existing is not null&&await db.GatewayPayments.AnyAsync(p=>p.RegistrationId==existing.Id&&p.Status=="Paid"))throw new ActivityException("This paid entry requires manual financial review before re-registration.");
        if(!RegistrationOpen(t))throw new ActivityException("Tournament registration is not open, is closed, or the draw has been locked.");if(team.Format!=field.Format)throw new ActivityException("Your team's format must match the tournament field.",400);
        if(await db.TournamentRegistrations.CountAsync(r=>r.TournamentId==id&&(r.Status=="Registered"||r.Status=="Pending"&&r.PaymentDeadline>rules.Now))>=t.Capacity)throw new ActivityException("Tournament registration is full.");
        if(existing is null)
        {
            var roster=await db.TeamMembers.Where(m=>m.TeamId==team.Id).OrderBy(m=>m.UserId).Select(m=>new{m.UserId,m.Position,m.User.FullName,m.User.Status,Player=db.UserRoles.Any(ur=>ur.UserId==m.UserId&&db.Roles.Any(r=>r.Id==ur.RoleId&&r.Name=="player"))}).ToListAsync();
            if(roster.Any(m=>m.Status!="Active"||!m.Player))throw new ActivityException("All players in the registered roster must be Active players.");var ids=roster.Select(m=>m.UserId).ToArray();
            if(await db.RegistrationPlayers.AnyAsync(p=>ids.Contains(p.UserId)&&p.Registration.TournamentId==id&&(p.Registration.Status=="Registered"||p.Registration.Status=="Pending"&&p.Registration.PaymentDeadline>rules.Now)&&p.Registration.TeamId!=team.Id))throw new ActivityException("A roster player already belongs to another registered entry in this tournament.");
            var captain=await db.Users.AsNoTracking().SingleAsync(u=>u.Id==actor);
            existing=new TournamentRegistration{TournamentId=id,TeamId=team.Id,CaptainId=actor,TeamNameSnapshot=team.Name,CaptainNameSnapshot=captain.FullName,EmailSnapshot=captain.Email!};
            foreach(var m in roster)existing.Players.Add(new(){UserId=m.UserId,NameSnapshot=m.FullName,Position=m.Position});db.TournamentRegistrations.Add(existing);
        }else
        {
            var ids=await db.RegistrationPlayers.Where(p=>p.RegistrationId==existing.Id).Select(p=>p.UserId).ToListAsync();
            if(await db.Users.AnyAsync(u=>ids.Contains(u.Id)&&(u.Status!="Active"||!db.UserRoles.Any(ur=>ur.UserId==u.Id&&db.Roles.Any(r=>r.Id==ur.RoleId&&r.Name=="player")))))throw new ActivityException("The frozen entry roster must contain Active players before rejoining.");
            if(await db.RegistrationPlayers.AnyAsync(p=>ids.Contains(p.UserId)&&p.Registration.TournamentId==id&&p.Registration.TeamId!=team.Id&&(p.Registration.Status=="Registered"||p.Registration.Status=="Pending"&&p.Registration.PaymentDeadline>rules.Now)))throw new ActivityException("A frozen roster player already belongs to another active entry.");
            existing.Status="Registered"; // Preserve original entry/roster snapshots and last withdrawal audit.
        }
        if(t.EntryFee>0){existing.Status="Pending";existing.PaymentDeadline=payments.Deadline(t.RegistrationClosesAt!.Value,t.EntryFee);}else{existing.Status="Registered";existing.PaymentDeadline=null;}
        await db.SaveChangesAsync();await payments.Add(existing,t.EntryFee,actor);await tx.CommitAsync();return false;
    }
    public async Task Withdraw(int actor,int id)
    {
        await using var tx=await db.Database.BeginTransactionAsync();var team=await CaptainTeam(actor);var(t,_)=await Gate(id);Modern(t);
        var r=await db.TournamentRegistrations.SingleOrDefaultAsync(r=>r.TournamentId==id&&r.TeamId==team.Id)??throw new ActivityException("Team entry not found.",404);
        if(r.Status=="Withdrawn"){await tx.CommitAsync();return;}
        if(!RegistrationOpen(t)||await db.TournamentMatches.AnyAsync(m=>m.TournamentId==id))throw new ActivityException("Teams may withdraw only before registration closes and before the draw is locked.");
        r.Status="Withdrawn";r.WithdrawnAt=rules.Now;r.WithdrawnBy=actor;await payments.Cancel(registrationId:r.Id);await db.SaveChangesAsync();await tx.CommitAsync();
    }
    public async Task Generate(int actor,int id,string version)
    {
        // Freeze the same team rows used by captain transfer; then follow field -> tournament order.
        var teams=await db.TournamentRegistrations.AsNoTracking().Where(r=>r.TournamentId==id&&r.Status=="Registered").Select(r=>r.TeamId).ToListAsync();
        await using var tx=await db.Database.BeginTransactionAsync();var locked=await LockTeams(teams);var(t,field)=await Gate(id);Owner(t,field,actor);await Editable(t);rules.Check(version,await Version(t));await Eligible(field.Id);
        var entries=await db.TournamentRegistrations.Where(r=>r.TournamentId==id&&r.Status=="Registered").OrderBy(r=>r.CreatedAt).ThenBy(r=>r.Id).ToListAsync();
        if(t.DrawStartedAt!=null||await db.TournamentMatches.AnyAsync(m=>m.TournamentId==id)||entries.Count<2||entries.Count>t.Capacity||rules.Now>=t.StartsAt||rules.Now<t.RegistrationClosesAt&&entries.Count<t.Capacity)throw new ActivityException("Lock the draw after registration closes or capacity is full, with at least two teams, before tournament start.");
        if(entries.Any(r=>!locked.Any(team=>team.Id==r.TeamId)))throw new ActivityException("Entries changed. Refresh before locking the draw.");foreach(var team in locked){await ValidTeam(team);if(team.Format!=field.Format)throw new ActivityException("A registered team changed format. Resolve the entry before locking the draw.");}
        var size=2;while(size<entries.Count)size*=2;var seeds=new int?[size];var half=size/2;
        for(var i=0;i<entries.Count;i++){var position=i<half?i*2:(i-half)*2+1;seeds[position]=entries[i].Id;db.TournamentDrawEntries.Add(new(){TournamentId=id,RegistrationId=entries[i].Id,SeedPosition=position+1});}
        List<TournamentMatche>? previous=null;var round=1;
        for(var count=size/2;count>=1;count/=2,round++)
        {
            var current=Enumerable.Range(1,count).Select(n=>new TournamentMatche{TournamentId=id,RoundNumber=round,MatchNumber=n,Status="Draft"}).ToList();db.TournamentMatches.AddRange(current);await db.SaveChangesAsync();
            for(var i=0;i<current.Count;i++)for(var slot=0;slot<2;slot++)db.TournamentMatchSlots.Add(new(){TournamentId=id,MatchId=current[i].Id,SlotNumber=slot+1,RegistrationId=previous is null?seeds[i*2+slot]:null,SourceMatchId=previous?[i*2+slot].Id});
            await db.SaveChangesAsync();previous=current;
        }
        t.DrawStartedAt=rules.Now;await ResolveByes(actor,id);await db.SaveChangesAsync();await tx.CommitAsync();
    }
    async Task ResolveByes(int actor,int id)
    {
        var matches=await db.TournamentMatches.Where(m=>m.TournamentId==id).Include(m=>m.InputSlots).OrderBy(m=>m.RoundNumber).ThenBy(m=>m.MatchNumber).ToListAsync();
        var byId=matches.ToDictionary(m=>m.Id);
        foreach(var m in matches)
        {
            if(m.Status!="Draft")continue;var slots=m.InputSlots.OrderBy(s=>s.SlotNumber).ToArray();
            foreach(var s in slots)if(s.SourceMatchId is int source&&byId[source].Status=="Completed")s.RegistrationId=byId[source].WinnerRegistrationId;
            if(slots.Length!=2||slots.Any(s=>s.SourceMatchId is int source&&byId[source].Status!="Completed"))continue;
            var participants=slots.Where(s=>s.RegistrationId!=null).ToArray();if(participants.Length!=1)continue;
            m.IsBye=true;m.WinnerRegistrationId=participants[0].RegistrationId;m.Status="Completed";m.UpdatedBy=actor;m.UpdatedAt=rules.Now;
        }
    }
}
