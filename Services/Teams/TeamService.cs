using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using KicksMvc.Data;
using KicksMvc.Models.Entities;
using KicksMvc.ViewModels;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
namespace KicksMvc.Services.Teams;

public sealed class TeamRuleException(string message,int status=409):Exception(message){public int Status {get;}=status;}
public sealed class TeamService(KicksDbContext db,IDataProtectionProvider protection)
{
    static readonly string[] Positions=["Goalkeeper","Defender","Midfielder","Forward"];
    readonly IDataProtector tokens=protection.CreateProtector("Kicks.Teams.Version.v1");
    static string Status(string status)=>status=="Declined"?"Rejected":status;
    string Protect(object value)=>tokens.Protect(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value)))));
    void Check(string? supplied,string expected){if(string.IsNullOrWhiteSpace(supplied))throw new TeamRuleException("Refresh this team before making changes.");try{if(tokens.Unprotect(supplied)!=tokens.Unprotect(expected))throw new TeamRuleException("This team or request changed. Refresh before trying again.");}catch(Exception error)when(error is CryptographicException or FormatException or ArgumentException){throw new TeamRuleException("This team or request changed. Refresh before trying again.");}}
    async Task<User?> LockUser(int id)=>await db.Users.FromSqlInterpolated($"SELECT * FROM dbo.users WITH(UPDLOCK,HOLDLOCK) WHERE id={id}").SingleOrDefaultAsync();
    async Task<User> Player(int id)
    {
        var user=await LockUser(id);
        if(user?.Status!="Active"||!await db.UserRoles.AnyAsync(ur=>ur.UserId==id&&db.Roles.Any(r=>r.Id==ur.RoleId&&r.Name=="player")))throw new TeamRuleException("The player must have an Active player account.");
        return user;
    }
    async Task<Team> Lock(int id)=>await db.Teams.FromSqlInterpolated($"SELECT * FROM dbo.teams WITH(UPDLOCK,HOLDLOCK) WHERE id={id}").SingleOrDefaultAsync()??throw new TeamRuleException("Team not found.",404);
    static void Captain(Team team,int actor){if(team.CaptainId!=actor)throw new TeamRuleException("Only this team's captain may perform this action.",403);}
    async Task<string> Version(Team team)
    {
        var members=await db.TeamMembers.AsNoTracking().Where(m=>m.TeamId==team.Id).OrderBy(m=>m.UserId).Select(m=>new{m.UserId,m.JoinedAt,m.Position}).ToListAsync();
        var watermark=await db.TeamRequests.Where(r=>r.TeamId==team.Id).MaxAsync(r=>(int?)r.Id)??0;
        return Protect(new{team.Id,team.CaptainId,team.Name,team.City,team.Area,team.Level,team.Format,team.Capacity,team.Bio,team.UsualMatchday,team.Initials,team.BadgeUrl,team.Color,team.IsRecruiting,members,watermark});
    }
    void Apply(Team team,TeamInput input)
    {
        if(input.Positions.Any(p=>!Positions.Contains(p)))throw new TeamRuleException("Choose valid recruiting positions.",400);
        team.Name=input.Name.Trim();if(team.Name.Length<3)throw new TeamRuleException("Team name must contain at least three characters.",400);
        team.City=input.City.Trim();if(team.City.Length==0)throw new TeamRuleException("Choose a city.",400);
        team.Area=input.Area?.Trim()??"";team.Level=input.Level;team.Format=input.Format;team.Capacity=input.Capacity;team.Bio=input.Bio?.Trim()??"";team.UsualMatchday=input.Schedule?.Trim()??"";team.Initials=input.Initials.ToUpperInvariant();team.Color=input.Color;team.IsRecruiting=input.Open;
    }
    async Task CancelOtherPending(int userId)=>await db.TeamRequests.Where(r=>r.UserId==userId&&r.Status=="Pending").ExecuteUpdateAsync(s=>s.SetProperty(r=>r.Status,"Cancelled").SetProperty(r=>r.ReviewedAt,DateTimeOffset.UtcNow).SetProperty(r=>r.ReviewedBy,userId));
    public async Task<int> Create(int actor,TeamInput input)
    {
        await using var tx=await db.Database.BeginTransactionAsync();await Player(actor);
        if(await db.TeamMembers.AnyAsync(m=>m.UserId==actor)||await db.Teams.AnyAsync(t=>t.CaptainId==actor))throw new TeamRuleException("Each player may belong to one team. Leave your current team first.");
        var team=new Team{CaptainId=actor};Apply(team,input);db.Teams.Add(team);await db.SaveChangesAsync();
        db.TeamMembers.Add(new(){UserId=actor,TeamId=team.Id});foreach(var p in input.Positions.Distinct())db.TeamRecruitingPositions.Add(new(){TeamId=team.Id,Position=p});
        await CancelOtherPending(actor);await db.SaveChangesAsync();await tx.CommitAsync();return team.Id;
    }
    public async Task Edit(int actor,int id,TeamInput input)
    {
        await using var tx=await db.Database.BeginTransactionAsync();var team=await Lock(id);Captain(team,actor);Check(input.Version,await Version(team));
        if(input.Capacity<await db.TeamMembers.CountAsync(m=>m.TeamId==id))throw new TeamRuleException("Capacity cannot be smaller than the current roster.");
        Apply(team,input);await db.TeamRecruitingPositions.Where(p=>p.TeamId==id).ExecuteDeleteAsync();foreach(var p in input.Positions.Distinct())db.TeamRecruitingPositions.Add(new(){TeamId=id,Position=p});await db.SaveChangesAsync();await tx.CommitAsync();
    }
    public async Task<int> Join(int actor,int id,TeamJoinInput input)
    {
        await using var tx=await db.Database.BeginTransactionAsync();var team=await Lock(id);await Player(actor);
        if(await db.TeamMembers.AnyAsync(m=>m.UserId==actor)||await db.Teams.AnyAsync(t=>t.CaptainId==actor))throw new TeamRuleException("You already belong to a team.");
        if(!team.IsRecruiting||await db.TeamMembers.CountAsync(m=>m.TeamId==id)>=team.Capacity)throw new TeamRuleException("This team is not currently accepting players.");
        if(await db.TeamRequests.AnyAsync(r=>r.TeamId==id&&r.UserId==actor&&r.RequestType=="join"&&r.Status=="Pending"))throw new TeamRuleException("You already have a pending request.");
        var request=new TeamRequest{TeamId=id,UserId=actor,Position=input.Position,Message=input.Message?.Trim()};db.TeamRequests.Add(request);await db.SaveChangesAsync();await tx.CommitAsync();return request.Id;
    }
    string RequestVersion(TeamRequest r)=>Protect(new{r.Id,r.TeamId,r.UserId,r.Status,r.RequestType,r.Position,r.ReviewedBy,r.ReviewedAt});
    public async Task Decide(int actor,int id,int requestId,bool accept,string version)
    {
        await using var tx=await db.Database.BeginTransactionAsync();var team=await Lock(id);Captain(team,actor);
        var request=await db.TeamRequests.SingleOrDefaultAsync(r=>r.Id==requestId&&r.TeamId==id)??throw new TeamRuleException("Request not found.",404);Check(version,RequestVersion(request));
        if(request.Status!="Pending")throw new TeamRuleException("This request was already resolved.");
        if(accept)await Player(request.UserId);else await LockUser(request.UserId);
        // Joining/creating another team may cancel this row while we wait for its
        // user's cross-team gate. Never review the pre-lock tracked snapshot.
        await db.Entry(request).ReloadAsync();Check(version,RequestVersion(request));
        if(request.Status!="Pending")throw new TeamRuleException("This request was already resolved.");
        if(request.RequestType=="leave")
        {
            if(request.UserId==team.CaptainId)throw new TeamRuleException("Transfer captaincy before leaving.");
            if(accept){var member=await db.TeamMembers.SingleOrDefaultAsync(m=>m.UserId==request.UserId&&m.TeamId==id)??throw new TeamRuleException("Membership changed.");db.TeamMembers.Remove(member);}
        }
        else if(accept)
        {
            if(await db.TeamMembers.AnyAsync(m=>m.UserId==request.UserId)||await db.Teams.AnyAsync(t=>t.CaptainId==request.UserId))throw new TeamRuleException("This player already belongs to a team.");
            if(await db.TeamMembers.CountAsync(m=>m.TeamId==id)>=team.Capacity)throw new TeamRuleException("The roster is full.");
            db.TeamMembers.Add(new(){UserId=request.UserId,TeamId=id,Position=request.Position});await CancelOtherPending(request.UserId);
        }
        request.Status=accept?"Accepted":"Declined";request.ReviewedBy=actor;request.ReviewedAt=DateTimeOffset.UtcNow;await db.SaveChangesAsync();await tx.CommitAsync();
    }
    public async Task CancelRequest(int actor,int requestId)
    {
        var id=await db.TeamRequests.Where(r=>r.Id==requestId&&r.UserId==actor).Select(r=>(int?)r.TeamId).SingleOrDefaultAsync()??throw new TeamRuleException("Request not found.",404);
        await using var tx=await db.Database.BeginTransactionAsync();await Lock(id);await Player(actor);
        var request=await db.TeamRequests.SingleAsync(r=>r.Id==requestId);if(request.Status!="Pending")throw new TeamRuleException("Request already resolved.");request.Status="Cancelled";request.ReviewedBy=actor;request.ReviewedAt=DateTimeOffset.UtcNow;await db.SaveChangesAsync();await tx.CommitAsync();
    }
    public async Task Remove(int actor,int id,int userId,string version,bool leave)
    {
        await using var tx=await db.Database.BeginTransactionAsync();var team=await Lock(id);if(leave){if(actor!=userId)throw new TeamRuleException("Invalid departure.",403);}else Captain(team,actor);
        Check(version,await Version(team));await LockUser(userId);if(userId==team.CaptainId)throw new TeamRuleException("The captain must transfer leadership before leaving.");
        var member=await db.TeamMembers.SingleOrDefaultAsync(m=>m.TeamId==id&&m.UserId==userId)??throw new TeamRuleException("Member not found.",404);
        db.TeamMembers.Remove(member);db.TeamRequests.Add(new(){TeamId=id,UserId=userId,RequestType="leave",Position=member.Position,Status="Accepted",Message=leave?"Player left the team.":"Member removed by captain.",ReviewedBy=actor,ReviewedAt=DateTimeOffset.UtcNow});await db.SaveChangesAsync();await tx.CommitAsync();
    }
    public async Task Transfer(int actor,int id,int target,string version)
    {
        await using var tx=await db.Database.BeginTransactionAsync();var team=await Lock(id);Captain(team,actor);Check(version,await Version(team));await Player(target);
        if(target==actor||!await db.TeamMembers.AnyAsync(m=>m.TeamId==id&&m.UserId==target))throw new TeamRuleException("Choose an existing ordinary member.");
        if(await db.Teams.AnyAsync(t=>t.CaptainId==target))throw new TeamRuleException("The selected player is already a captain.");team.CaptainId=target;await db.SaveChangesAsync();await tx.CommitAsync();
    }
    public async Task<object> Detail(int actor,int id,int page=1)
    {
        var team=await db.Teams.AsNoTracking().Include(t=>t.Captain).Include(t=>t.Members).ThenInclude(m=>m.User).Include(t=>t.RecruitingPositions).AsSplitQuery().SingleOrDefaultAsync(t=>t.Id==id)??throw new TeamRuleException("Team not found.",404);
        var captain=team.CaptainId==actor;
        var query=db.TeamRequests.AsNoTracking().Where(r=>r.TeamId==id&&(captain||r.UserId==actor));
        var total=await query.CountAsync();var requests=await query.OrderByDescending(r=>r.Id).Skip((page-1)*20).Take(20).Select(r=>new{Request=r,Name=db.Users.Where(u=>u.Id==r.UserId).Select(u=>u.FullName).First()}).ToListAsync();
        return new{team.Id,team.Name,team.City,team.Area,team.Level,team.Format,team.Capacity,team.Bio,Schedule=team.UsualMatchday,team.Initials,team.Color,Logo=SafeLogo(team.BadgeUrl),Open=team.IsRecruiting,CaptainId=team.CaptainId,Captain=team.Captain.FullName,IsCaptain=captain,IsMember=team.Members.Any(m=>m.UserId==actor),Positions=team.RecruitingPositions.Select(p=>p.Position),Members=team.Members.OrderBy(m=>m.JoinedAt).Select(m=>new{Id=m.UserId,Name=m.User.FullName,m.Position,Role=m.UserId==team.CaptainId?"Captain":"Member"}),Version=await Version(team),Requests=requests.Select(r=>new{r.Request.Id,r.Request.UserId,r.Name,r.Request.Position,r.Request.Message,Type=r.Request.RequestType,Status=Status(r.Request.Status),Version=RequestVersion(r.Request)}),RequestTotal=total,Page=page};
    }
    public static string? SafeLogo(string? value)=>value is not null&&System.Text.RegularExpressions.Regex.IsMatch(value,@"^/media/teams/[a-f0-9]{32}\.(png|jpg|webp)$")?value:null;
    public async Task<object> List(int actor,TeamListInput input)
    {
        var membership=await db.TeamMembers.AsNoTracking().Where(m=>m.UserId==actor).Select(m=>(int?)m.TeamId).SingleOrDefaultAsync();
        var captain=await db.Teams.Where(t=>t.CaptainId==actor).Select(t=>(int?)t.Id).SingleOrDefaultAsync();
        var query=db.Teams.AsNoTracking();if(input.Tab=="my")query=query.Where(t=>t.Id==membership||t.CaptainId==actor);
        if(input.Tab=="requests")query=query.Where(t=>db.TeamRequests.Any(r=>r.TeamId==t.Id&&r.UserId==actor));
        if(!string.IsNullOrWhiteSpace(input.Search)){var s=input.Search.Trim();query=query.Where(t=>t.Name.Contains(s)||t.City.Contains(s)||t.Area.Contains(s));}
        if(!string.IsNullOrWhiteSpace(input.City))query=query.Where(t=>t.City==input.City);if(!string.IsNullOrWhiteSpace(input.Level))query=query.Where(t=>t.Level==input.Level);if(!string.IsNullOrWhiteSpace(input.Format))query=query.Where(t=>t.Format==input.Format);
        if(input.Recruiting)query=query.Where(t=>t.IsRecruiting&&t.Members.Count<t.Capacity);
        var total=await query.CountAsync();var page=Math.Min(input.Page,Math.Max(1,(int)Math.Ceiling(total/12d)));query=input.Sort=="newest"?query.OrderByDescending(t=>t.Id):input.Sort=="name"?query.OrderBy(t=>t.Name).ThenBy(t=>t.Id):query.OrderByDescending(t=>t.IsRecruiting).ThenBy(t=>t.Name).ThenBy(t=>t.Id);
        var teams=await query.Skip((page-1)*12).Take(12).Select(t=>new{t.Id,t.Name,t.City,t.Area,t.Level,t.Format,t.Capacity,t.Initials,t.Color,t.BadgeUrl,t.Bio,Schedule=t.UsualMatchday,Captain=t.Captain.FullName,IsCaptain=t.CaptainId==actor,IsMember=t.Members.Any(m=>m.UserId==actor),Count=t.Members.Count,Open=t.IsRecruiting,RequestStatus=db.TeamRequests.Where(r=>r.TeamId==t.Id&&r.UserId==actor&&r.RequestType=="join").OrderByDescending(r=>r.Id).Select(r=>r.Status).FirstOrDefault()}).ToListAsync();
        return new{Items=teams.Select(t=>new{t.Id,t.Name,t.City,t.Area,t.Level,t.Format,t.Capacity,t.Initials,t.Color,Logo=SafeLogo(t.BadgeUrl),t.Bio,t.Schedule,t.Captain,t.IsCaptain,t.IsMember,t.Count,t.Open,RequestStatus=t.RequestStatus is null?null:Status(t.RequestStatus)}),Total=total,Page=page,PageSize=12,MyTeam=membership??captain,PendingRequests=await db.TeamRequests.CountAsync(r=>r.UserId==actor&&r.Status=="Pending")};
    }
    public async Task<string?> SetLogo(int actor,int id,string version,string logo)
    {
        await using var tx=await db.Database.BeginTransactionAsync();var team=await Lock(id);Captain(team,actor);Check(version,await Version(team));var old=SafeLogo(team.BadgeUrl);team.BadgeUrl=logo;await db.SaveChangesAsync();await tx.CommitAsync();return old;
    }
}
