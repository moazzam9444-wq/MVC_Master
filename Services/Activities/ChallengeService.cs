using KicksMvc.Data;
using KicksMvc.Models.Entities;
using KicksMvc.Services.Bookings;
using KicksMvc.Services.Fields;
using KicksMvc.ViewModels;
using Microsoft.EntityFrameworkCore;
namespace KicksMvc.Services.Activities;
public sealed class ChallengeService(KicksDbContext db,FieldService fields,ActivityRules rules)
{
    async Task<List<Team>> LockTeams(int first,int? second){var result=new List<Team>();foreach(var id in new[]{first,second??first}.Distinct().Order()){result.Add(await db.Teams.FromSqlInterpolated($"SELECT * FROM dbo.teams WITH(UPDLOCK,HOLDLOCK) WHERE id={id}").SingleOrDefaultAsync()??throw new ActivityException("Team not found.",404));}return result;}
    async Task ValidCaptain(Team t){if(!await db.TeamMembers.AnyAsync(m=>m.TeamId==t.Id&&m.UserId==t.CaptainId)||!await db.Users.AnyAsync(u=>u.Id==t.CaptainId&&u.Status=="Active")||!await db.UserRoles.AnyAsync(ur=>ur.UserId==t.CaptainId&&db.Roles.Any(r=>r.Id==ur.RoleId&&r.Name=="player")))throw new ActivityException("Both teams must have an Active player captain and valid roster.");}
    string Version(Challenge c)=>rules.Version(new{c.Id,c.HostTeamId,c.OpponentTeamId,c.FieldId,c.StartsAt,c.DurationMinutes,c.Status,c.QuotedPrice,c.Note,c.Format});
    public async Task<int> Create(int actor,ChallengeInput input)
    {
        var start=rules.Start(input);var hostId=await db.Teams.Where(t=>t.CaptainId==actor).Select(t=>(int?)t.Id).SingleOrDefaultAsync()??throw new ActivityException("Only a current team captain may send a challenge.",403);
        if(hostId==input.TargetTeamId)throw new ActivityException("A team cannot challenge itself.",400);
        await using var tx=await db.Database.BeginTransactionAsync();var locked=await LockTeams(hostId,input.TargetTeamId);var host=locked.Single(t=>t.Id==hostId);var target=locked.Single(t=>t.Id==input.TargetTeamId);if(host.CaptainId!=actor)throw new ActivityException("Your captaincy changed. Refresh.",403);await ValidCaptain(host);await ValidCaptain(target);if(target.Format!=host.Format)throw new ActivityException("Choose a team with the same match format.",400);
        if(await db.Challenges.AnyAsync(c=>c.StartsAt==start&&(c.Status=="Open"||c.Status=="Agreed"||c.Status=="Booked")&&((c.HostTeamId==host.Id&&c.OpponentTeamId==target.Id)||(c.HostTeamId==target.Id&&c.OpponentTeamId==host.Id))))throw new ActivityException("An active challenge already exists for these teams and start time.");
        Field? field=null;decimal? price=null;if(input.FieldId is int fieldId){await FieldOccupancy.Lock(db,fieldId);field=await fields.PublicFields.AsNoTracking().SingleOrDefaultAsync(f=>f.Id==fieldId)??throw new ActivityException("Field unavailable.",404);if(field.Format!=host.Format)throw new ActivityException("Choose a field matching the team format.",400);if(await FieldOccupancy.Occupied(db,fieldId,start,start.AddMinutes(input.DurationMinutes)))throw new ActivityException("The proposed field interval is unavailable.");price=ActivityRules.Price(field.HourlyPrice,input.DurationMinutes);}
        var challenge=new Challenge{HostTeamId=host.Id,OpponentTeamId=target.Id,PostedBy=actor,FieldId=field?.Id,StartsAt=start,DurationMinutes=input.DurationMinutes,City=field?.City??host.City,Format=host.Format,Level=host.Level,PaymentMode="Legacy full",QuotedPrice=price,Note=input.Note?.Trim()};db.Challenges.Add(challenge);await db.SaveChangesAsync();db.ChallengeRequests.Add(new(){ChallengeId=challenge.Id,TeamId=target.Id,Note=input.Note?.Trim()});await db.SaveChangesAsync();await tx.CommitAsync();return challenge.Id;
    }
    public async Task Respond(int actor,int id,string decision,string version)
    {
        var identity=await db.Challenges.AsNoTracking().Where(c=>c.Id==id).Select(c=>new{c.HostTeamId,c.OpponentTeamId}).SingleOrDefaultAsync()??throw new ActivityException("Challenge not found.",404);
        await using var tx=await db.Database.BeginTransactionAsync();var locked=await LockTeams(identity.HostTeamId,identity.OpponentTeamId);var host=locked.Single(t=>t.Id==identity.HostTeamId);var target=locked.SingleOrDefault(t=>t.Id==identity.OpponentTeamId);
        if(decision=="withdraw"?host.CaptainId!=actor:target?.CaptainId!=actor)throw new ActivityException("Only the authorized current captain may respond.",403);
        var c=await db.Challenges.SingleAsync(c=>c.Id==id);rules.Check(version,Version(c));if(c.Status!="Open"||c.StartsAt<=rules.Now||!rules.Valid(c.StartsAt,c.DurationMinutes))throw new ActivityException("This proposal is resolved or expired.");
        var request=await db.ChallengeRequests.SingleOrDefaultAsync(r=>r.ChallengeId==id&&r.TeamId==c.OpponentTeamId&&r.Status=="Pending");
        if(decision=="withdraw"){c.Status="Cancelled";if(request is not null)request.Status="Withdrawn";}
        else if(decision=="reject"){c.Status="Cancelled";if(request is null)throw new ActivityException("Target proposal unavailable.");request.Status="Declined";}
        else
        {
            if(target is null||request is null)throw new ActivityException("Target team unavailable.");await ValidCaptain(host);await ValidCaptain(target);if(host.Format!=c.Format||target.Format!=c.Format)throw new ActivityException("Team format changed. Send a new proposal.");
            if(c.FieldId is int fieldId)
            {
                await FieldOccupancy.Lock(db,fieldId);var field=await fields.PublicFields.AsNoTracking().SingleOrDefaultAsync(f=>f.Id==fieldId)??throw new ActivityException("The field or its owner is unavailable.");
                if(field.Format!=c.Format)throw new ActivityException("Field format changed. Send a new proposal.");var price=ActivityRules.Price(field.HourlyPrice,c.DurationMinutes);if(c.QuotedPrice!=price)throw new ActivityException("Field price changed. Send a new proposal with the current price.");
                if(await FieldOccupancy.Occupied(db,fieldId,c.StartsAt,c.StartsAt.AddMinutes(c.DurationMinutes)))throw new ActivityException("The field interval was reserved. The challenge was not accepted or booked.");
                var booking=new Booking{ChallengeId=c.Id,UserId=host.CaptainId,OwnerId=field.OwnerId,FieldId=field.Id,StartsAt=c.StartsAt,DurationMinutes=c.DurationMinutes,Price=price,OwnerNet=price,CommissionRate=0,Commission=0,IsSplit=false,Status="Confirmed"};db.Bookings.Add(booking);await db.SaveChangesAsync();db.BookingTeams.AddRange(new BookingTeam{BookingId=booking.Id,TeamId=host.Id,CaptainId=host.CaptainId,TeamNameSnapshot=host.Name},new BookingTeam{BookingId=booking.Id,TeamId=target.Id,CaptainId=target.CaptainId,TeamNameSnapshot=target.Name});c.Status="Booked";
            }else c.Status="Agreed";
            request.Status="Accepted";
        }
        await db.SaveChangesAsync();await tx.CommitAsync();
    }
    public async Task<object> Detail(int actor,int id)
    {
        var c=await db.Challenges.AsNoTracking().Include(c=>c.HostTeam).Include(c=>c.OpponentTeam).Include(c=>c.Field).Include(c=>c.Booking).Include(c=>c.Requests).AsSplitQuery().SingleOrDefaultAsync(c=>c.Id==id)??throw new ActivityException("Challenge not found.",404);
        var host=c.HostTeam.CaptainId==actor;var target=c.OpponentTeam?.CaptainId==actor;if(!host&&!target)throw new ActivityException("Challenge not found.",404);var req=c.Requests.FirstOrDefault(r=>r.TeamId==c.OpponentTeamId);
        var status=c.Status=="Open"?(c.StartsAt<=rules.Now?"Expired":"Pending"):c.Status is "Agreed" or "Booked"?"Accepted":req?.Status=="Declined"?"Rejected":c.Status=="Cancelled"?"Cancelled":c.Status;
        return new{c.Id,Host=c.HostTeam.Name,Target=c.OpponentTeam?.Name,c.HostTeamId,c.OpponentTeamId,c.StartsAt,EndsAt=c.StartsAt.AddMinutes(c.DurationMinutes),c.DurationMinutes,c.FieldId,Field=c.Field?.Name,c.QuotedPrice,c.Note,Status=status,IsHost=host,CanRespond=target&&status=="Pending",CanWithdraw=host&&status=="Pending",Version=Version(c),Reservation=c.Booking is null?null:new{c.Booking.Id,c.Booking.Status,c.Booking.Price},Reserved=c.Booking?.Status is "Pending" or "Confirmed",NoPayment=true};
    }
    public async Task<object> List(int actor,ActivityListInput input)
    {
        var q=db.Challenges.AsNoTracking().Where(c=>c.HostTeam.CaptainId==actor||c.OpponentTeam!=null&&c.OpponentTeam.CaptainId==actor);
        if(input.Mode=="sent")q=q.Where(c=>c.HostTeam.CaptainId==actor);if(input.Mode=="received")q=q.Where(c=>c.OpponentTeam!=null&&c.OpponentTeam.CaptainId==actor);
        if(!string.IsNullOrWhiteSpace(input.Search)){var s=input.Search.Trim();q=q.Where(c=>c.HostTeam.Name.Contains(s)||c.OpponentTeam!=null&&c.OpponentTeam.Name.Contains(s));}
        var total=await q.CountAsync();var page=Math.Min(input.Page,Math.Max(1,(int)Math.Ceiling(total/12d)));var ids=await q.OrderByDescending(c=>c.Id).Skip((page-1)*12).Take(12).Select(c=>c.Id).ToListAsync();var items=new List<object>();foreach(var id in ids)items.Add(await Detail(actor,id));return new{Items=items,Total=total,Page=page,PageSize=12,OwnTeam=await db.Teams.Where(t=>t.CaptainId==actor).Select(t=>new{t.Id,t.Name,t.Format}).SingleOrDefaultAsync()};
    }
}
