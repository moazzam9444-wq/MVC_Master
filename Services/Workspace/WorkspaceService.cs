using KicksMvc.Data;
using KicksMvc.Models.Entities;
using KicksMvc.ViewModels;
using Microsoft.EntityFrameworkCore;
namespace KicksMvc.Services.Workspace;

// Read-only projections: all totals are SQL aggregates, all detail lists are bounded.
public sealed class WorkspaceService(KicksDbContext db, TimeProvider clock)
{
    public async Task<WorkspaceOverview> Overview(int actor, string role)
    {
        var now=clock.GetUtcNow();
        var bookings=db.Bookings.AsNoTracking().Where(b=>role=="admin" || (role=="owner" ? b.OwnerId==actor && b.Field.OwnerId==actor : b.UserId==actor));
        var games=db.OpenGames.AsNoTracking().Where(g=>g.Status=="Scheduled" && g.EndsAt>now &&
            (role=="admin" || (role=="owner" ? g.OrganizerId==actor : g.Participants.Any(p=>p.UserId==actor && p.Status=="Joined"))));
        var tournaments=db.Tournaments.AsNoTracking().Where(t=>t.Status!="Cancelled" && t.Status!="Archived" && t.Status!="Completed" && t.EndsAt>now &&
            (role=="admin" || (role=="owner" ? t.CreatedBy==actor : t.Registrations.Any(r=>r.Players.Any(p=>p.UserId==actor) && r.Status=="Registered"))));
        var metrics=new List<WorkspaceMetric>();
        if(role=="admin")
        {
            metrics.Add(new("Accounts",await db.Users.CountAsync(),"All account statuses","#users"));
            metrics.Add(new("Awaiting approval",await db.Users.CountAsync(u=>u.Status=="Pending"),"Player and owner requests","#users"));
            metrics.Add(new("Fields",await db.Fields.CountAsync(),"Published and archived","#fields"));
            metrics.Add(new("Payment reviews",await db.GatewayPayments.CountAsync(p=>p.FinancialReview),"Stripe sandbox · manual review","#revenue"));
        }
        else
        {
            if(role=="owner")metrics.Add(new("Your fields",await db.Fields.CountAsync(f=>f.OwnerId==actor),"Published and archived","#fields"));
            metrics.Add(new("Bookings",await bookings.CountAsync(),"All reservation statuses","#bookings"));
            metrics.Add(new("Open games",await games.CountAsync(),role=="owner"?"Your active games":"Your active joined games","#memberships"));
            if(role=="owner")metrics.Add(new("Players joined",await db.GameParticipants.CountAsync(p=>p.Status=="Joined" && games.Any(g=>g.Id==p.GameId)),"Across your active Open Games","#memberships"));
            metrics.Add(new("Tournaments",await tournaments.CountAsync(),role=="owner"?"Your active tournaments":"Your active team entries",role=="owner"?"#tournaments":"#registrations"));
        }
        var groups=await bookings.GroupBy(b=>b.Status).Select(g=>new {Status=g.Key,Count=g.Count()}).ToListAsync();
        var statuses=new[]{"Pending","Confirmed","Completed","Cancelled"}.Select(s=>new WorkspaceMetric(s,groups.FirstOrDefault(g=>g.Status==s)?.Count??0,"All bookings","#bookings")).ToList();
        var recent=await bookings.OrderByDescending(b=>b.CreatedAt).ThenByDescending(b=>b.Id).Take(5)
            .Select(b=>new WorkspaceRow(b.Field.Name,"#"+b.Id+" · "+b.DurationMinutes+" min · "+b.Price.ToString()+" JOD",b.StartsAt,b.Status,"#bookings")).ToListAsync();
        var activity=await games.OrderBy(g=>g.StartsAt).ThenBy(g=>g.Id).Take(3)
            .Select(g=>new WorkspaceRow(g.Title,g.Field.Name,g.StartsAt,"Open game","/Pages/OpenGames/game-details.html?id="+g.Id)).ToListAsync();
        activity.AddRange(await tournaments.OrderBy(t=>t.StartsAt).ThenBy(t=>t.Id).Take(3)
            .Select(t=>new WorkspaceRow(t.Name,t.Field.Name,t.StartsAt,"Tournament","/Pages/Tournaments/tournament-details.html?id="+t.Id)).ToListAsync());
        var team=role=="player"?await db.Teams.AsNoTracking().Where(t=>t.CaptainId==actor||t.Members.Any(m=>m.UserId==actor)).Select(t=>new{t.Name,t.Id,t.CaptainId}).FirstOrDefaultAsync():null;
        var requests=team is null?0:await db.TeamRequests.CountAsync(r=>r.Status=="Pending" && (team.CaptainId==actor?r.TeamId==team.Id:r.UserId==actor));
        return new(role,metrics,statuses,recent,activity.OrderBy(a=>a.Date).Take(5).ToList(),team?.Name,requests);
    }

    public async Task<WorkspaceLedger> Ledger(string kind,int page,string? search)
    {
        page=Math.Clamp(page,1,100000);search=search?.Trim();if(search?.Length>120)search=search[..120];
        if(kind=="fields")
        {
            var fields=db.Fields.AsNoTracking();if(!string.IsNullOrEmpty(search))fields=fields.Where(f=>f.Name.Contains(search)||f.City.Contains(search)||f.Owner.FullName.Contains(search));
            var total=await fields.CountAsync();page=Math.Min(page,Math.Max(1,(total+19)/20));
            return new(kind,page,total,await fields.OrderByDescending(f=>f.Id).Skip((page-1)*20).Take(20).Select(f=>new WorkspaceRow(f.Name,f.City+" · "+f.Format+" · "+f.HourlyPrice.ToString()+" JOD / hour",f.CreatedAt,f.IsArchived?"Archived":f.Owner.Status=="Active"?"Published":"Owner inactive",f.IsArchived||f.Owner.Status!="Active"?null:"/Pages/Fields/field-details.html?id="+f.Id,f.Owner.FullName)).ToListAsync());
        }
        if(kind=="feedback")
        {
            var q=db.Feedback.AsNoTracking();if(!string.IsNullOrEmpty(search))q=q.Where(f=>f.Name.Contains(search)||f.Message.Contains(search)||f.Email.Contains(search));
            var total=await q.CountAsync();page=Math.Min(page,Math.Max(1,(total+19)/20));
            return new(kind,page,total,await q.OrderByDescending(f=>f.Id).Skip((page-1)*20).Take(20).Select(f=>new WorkspaceRow(f.Name,f.Message,f.CreatedAt,f.Rating+" / 5",null,f.Email)).ToListAsync());
        }
        var b=db.Bookings.AsNoTracking();if(!string.IsNullOrEmpty(search))b=b.Where(x=>x.Field.Name.Contains(search)||x.Player.FullName.Contains(search)||x.Status.Contains(search));
        var count=await b.CountAsync();page=Math.Min(page,Math.Max(1,(count+19)/20));
        return new("bookings",page,count,await b.OrderByDescending(x=>x.StartsAt).ThenByDescending(x=>x.Id).Skip((page-1)*20).Take(20).Select(x=>new WorkspaceRow(x.Field.Name,"#"+x.Id+" · "+x.Player.FullName+" · "+x.DurationMinutes+" min",x.StartsAt,x.Status,"/Pages/Fields/field-details.html?id="+x.FieldId)).ToListAsync());
    }

    // Never let browser-provided roles, IDs, or message history grant contact access.
    public IQueryable<User> Contacts(int actor)
    {
        var admins=db.UserRoles.Where(ur=>db.Roles.Any(r=>r.Id==ur.RoleId && r.Name=="admin")).Select(ur=>ur.UserId);
        return db.Users.AsNoTracking().Where(u=>u.Id!=actor && u.Status=="Active" &&
            (admins.Contains(actor)||admins.Contains(u.Id)||
             db.Bookings.Any(b=>(b.Status=="Pending"||b.Status=="Confirmed"||b.Status=="Completed") && ((b.UserId==actor&&b.OwnerId==u.Id)||(b.OwnerId==actor&&b.UserId==u.Id)))||
             db.Challenges.Any(c=>(c.Status=="Agreed"||c.Status=="Booked"||c.Status=="Completed"||c.Status=="Awaiting payment") &&
                ((c.HostTeam.CaptainId==actor&&c.OpponentTeam!=null&&c.OpponentTeam.CaptainId==u.Id)||(c.HostTeam.CaptainId==u.Id&&c.OpponentTeam!=null&&c.OpponentTeam.CaptainId==actor)))));
    }
}
