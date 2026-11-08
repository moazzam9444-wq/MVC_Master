using KicksMvc.Data;
using KicksMvc.Models.Entities;
using KicksMvc.Services.Bookings;
using KicksMvc.Services.Fields;
using KicksMvc.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
using System.Text.Json;
using System.Security.Cryptography;
namespace KicksMvc.Services.Activities;
public sealed class OpenGameService(KicksDbContext db,FieldService fields,ActivityRules rules,IDataProtectionProvider protection)
{
    readonly IDataProtector quotes=protection.CreateProtector("Kicks.OpenGameCreation.v1");
    sealed record Quote(int Actor,int FieldId,DateTimeOffset Start,int Duration,int Capacity,string Title,string Level,int Watermark,DateTimeOffset Expiry);
    string Version(OpenGame g)=>rules.Version(new{g.Id,g.OrganizerId,g.FieldId,g.StartsAt,g.DurationMinutes,g.Capacity,g.Title,g.Level,g.Status});
    async Task<Field> Eligible(int actor,int id)=>await fields.PublicFields.AsNoTracking().SingleOrDefaultAsync(f=>f.Id==id&&f.OwnerId==actor)??throw new ActivityException("Choose an eligible field that you own.",404);
    async Task<User> ActivePlayer(int id){var user=await db.Users.FromSqlInterpolated($"SELECT * FROM dbo.users WITH(UPDLOCK,HOLDLOCK) WHERE id={id}").SingleOrDefaultAsync();if(user?.Status!="Active"||!await db.UserRoles.AnyAsync(ur=>ur.UserId==id&&db.Roles.Any(r=>r.Id==ur.RoleId&&r.Name=="player")))throw new ActivityException("An Active player account is required.",403);return user;}
    async Task<OpenGame> Gate(int id){var field=await db.OpenGames.Where(g=>g.Id==id).Select(g=>(int?)g.FieldId).SingleOrDefaultAsync()??throw new ActivityException("Game not found.",404);await FieldOccupancy.Lock(db,field);return await db.OpenGames.SingleAsync(g=>g.Id==id);}
    static void Owner(OpenGame g,int actor,Field field){if(g.OrganizerId!=actor||field.OwnerId!=actor)throw new ActivityException("You may manage only games on your own fields.",403);}
    void Upcoming(OpenGame g){if(g.Status!="Scheduled"||g.StartsAt<=rules.Now||!rules.Valid(g.StartsAt,g.DurationMinutes))throw new ActivityException("This game has started, ended, been cancelled, or is not joinable.");}
    public async Task<object> QuoteGame(int actor,GameInput input)
    {
        var start=rules.Start(input);await using var tx=await db.Database.BeginTransactionAsync();await FieldOccupancy.Lock(db,input.FieldId);await Eligible(actor,input.FieldId);
        if(await FieldOccupancy.Occupied(db,input.FieldId,start,start.AddMinutes(input.DurationMinutes)))throw new ActivityException("This field interval is unavailable.");
        var watermark=await db.OpenGames.Where(g=>g.OrganizerId==actor&&g.FieldId==input.FieldId&&g.StartsAt==start&&g.DurationMinutes==input.DurationMinutes).MaxAsync(g=>(int?)g.Id)??0;
        var q=new Quote(actor,input.FieldId,start,input.DurationMinutes,input.Capacity,string.IsNullOrWhiteSpace(input.Title)?"Open game":input.Title.Trim(),input.Level?.Trim()??"All levels",watermark,rules.Now.AddMinutes(15));await tx.CommitAsync();return new{Token=quotes.Protect(JsonSerializer.Serialize(q)),StartsAt=q.Start,EndsAt=q.Start.AddMinutes(q.Duration),DurationMinutes=q.Duration,q.Capacity,Payment="No payment collected"};
    }
    public async Task<(int Id,bool Replay)> Create(int actor,string token)
    {
        Quote q;try{q=JsonSerializer.Deserialize<Quote>(quotes.Unprotect(token))??throw new CryptographicException();}catch(Exception e)when(e is CryptographicException or FormatException or ArgumentException or JsonException){throw new ActivityException("Invalid creation quote. Select the session again.",400);}
        if(q.Actor!=actor)throw new ActivityException("This quote belongs to another owner.",403);
        await using var tx=await db.Database.BeginTransactionAsync();await FieldOccupancy.Lock(db,q.FieldId);
        var existing=await db.OpenGames.Where(g=>g.OrganizerId==actor&&g.FieldId==q.FieldId&&g.StartsAt==q.Start&&g.DurationMinutes==q.Duration&&g.Id>q.Watermark).OrderBy(g=>g.Id).FirstOrDefaultAsync();if(existing is not null){await tx.CommitAsync();return(existing.Id,true);}
        if(q.Expiry<rules.Now||q.Start<=rules.Now||!rules.Valid(q.Start,q.Duration)||q.Capacity is <2 or >30)throw new ActivityException("This creation quote expired or is invalid.");await Eligible(actor,q.FieldId);
        if(await FieldOccupancy.Occupied(db,q.FieldId,q.Start,q.Start.AddMinutes(q.Duration)))throw new ActivityException("This field interval was just reserved.");
        var game=new OpenGame{OrganizerId=actor,FieldId=q.FieldId,StartsAt=q.Start,DurationMinutes=q.Duration,Capacity=q.Capacity,Title=q.Title,Level=q.Level,PlayerFee=0};db.OpenGames.Add(game);await db.SaveChangesAsync();await tx.CommitAsync();return(game.Id,false);
    }
    public async Task<bool> Join(int actor,int id,string position)
    {
        await using var tx=await db.Database.BeginTransactionAsync();var game=await Gate(id);Upcoming(game);if(!await fields.PublicFields.AnyAsync(f=>f.Id==game.FieldId))throw new ActivityException("The field or owner is unavailable.");await ActivePlayer(actor);
        var row=await db.GameParticipants.SingleOrDefaultAsync(p=>p.GameId==id&&p.UserId==actor);if(row?.Status=="Joined"){await tx.CommitAsync();return true;}
        if(await db.GameParticipants.CountAsync(p=>p.GameId==id&&p.Status=="Joined")>=game.Capacity)throw new ActivityException("This game is full.");
        if(row is null)db.GameParticipants.Add(new(){GameId=id,UserId=actor,Position=position});else{row.Status="Joined";row.Position=position;}await db.SaveChangesAsync();await tx.CommitAsync();return false;
    }
    public async Task Leave(int actor,int id)
    {
        await using var tx=await db.Database.BeginTransactionAsync();var g=await Gate(id);await ActivePlayer(actor);var row=await db.GameParticipants.SingleOrDefaultAsync(p=>p.GameId==id&&p.UserId==actor)??throw new ActivityException("Participation not found.",404);
        if(row.Status=="Left"){await tx.CommitAsync();return;}Upcoming(g);if(g.StartsAt-rules.Now<TimeSpan.FromHours(1))throw new ActivityException("Players may leave only when at least one hour remains before the Amman start.");row.Status="Left";await db.SaveChangesAsync();await tx.CommitAsync();
    }
    public async Task Manage(int actor,int id,string version,GameEditInput? input)
    {
        await using var tx=await db.Database.BeginTransactionAsync();var game=await Gate(id);Owner(game,actor,await db.Fields.SingleAsync(f=>f.Id==game.FieldId));rules.Check(version,Version(game));Upcoming(game);
        if(input is null)game.Status="Cancelled";else{await Eligible(actor,game.FieldId);if(input.Capacity<await db.GameParticipants.CountAsync(p=>p.GameId==id&&p.Status=="Joined"))throw new ActivityException("Capacity cannot be below the active participant count.");game.Title=input.Title.Trim();if(game.Title.Length==0)throw new ActivityException("Enter a title.",400);game.Capacity=input.Capacity;game.Level=input.Level?.Trim()??"All levels";}
        await db.SaveChangesAsync();await tx.CommitAsync();
    }
    public async Task<object> Details(int actor,int id)
    {
        var g=await db.OpenGames.AsNoTracking().Include(g=>g.Field).SingleOrDefaultAsync(g=>g.Id==id)??throw new ActivityException("Game not found.",404);var owner=g.OrganizerId==actor&&g.Field.OwnerId==actor;var eligible=await fields.PublicFields.AnyAsync(f=>f.Id==g.FieldId);if(!eligible&&!owner&&!await db.GameParticipants.AnyAsync(p=>p.GameId==id&&p.UserId==actor))throw new ActivityException("Game not found.",404);
        var count=await db.GameParticipants.CountAsync(p=>p.GameId==id&&p.Status=="Joined");var mine=await db.GameParticipants.Where(p=>p.GameId==id&&p.UserId==actor).Select(p=>p.Status).SingleOrDefaultAsync();var upcoming=g.StartsAt>rules.Now&&g.Status=="Scheduled";
        var participants=owner?await db.GameParticipants.Where(p=>p.GameId==id&&p.Status=="Joined").OrderBy(p=>p.Id).Select(p=>new{Id=p.UserId,Name=p.User.FullName,p.Position,p.Status}).Take(30).ToListAsync():null;
        var image=await db.FieldImages.Where(i=>i.FieldId==g.FieldId).OrderBy(i=>i.SortOrder).ThenBy(i=>i.Id).Select(i=>i.StorageUrl).FirstOrDefaultAsync();
        return new{g.Id,g.Title,g.FieldId,Field=g.Field.Name,g.Field.City,g.Field.Format,Image=image is null?"/assets/images/field1.jpg":"/media/fields/"+Uri.EscapeDataString(image),g.StartsAt,g.EndsAt,g.DurationMinutes,g.Capacity,g.Level,Count=count,Joined=mine=="Joined",Participation=mine,IsOwner=owner,Status=g.Status=="Cancelled"?"Cancelled":g.EndsAt<=rules.Now?"Completed":g.StartsAt<=rules.Now?"In progress":count>=g.Capacity?"Full":"Open",CanJoin=eligible&&upcoming&&rules.Valid(g.StartsAt,g.DurationMinutes)&&count<g.Capacity&&mine!="Joined",CanLeave=upcoming&&mine=="Joined"&&g.StartsAt-rules.Now>=TimeSpan.FromHours(1),CanManage=owner&&upcoming,Version=owner?Version(g):null,Participants=participants,NoPayment=true};
    }
    public async Task<object> List(int actor,bool owner,ActivityListInput input)
    {
        var q=db.OpenGames.AsNoTracking();if(input.Mode=="mine"){if(!owner)throw new ActivityException("Owner access required.",403);q=q.Where(g=>g.OrganizerId==actor&&g.Field.OwnerId==actor);}else if(input.Mode=="joined")q=q.Where(g=>db.GameParticipants.Any(p=>p.GameId==g.Id&&p.UserId==actor));else q=q.Where(g=>g.Status=="Scheduled"&&g.StartsAt>rules.Now&&fields.PublicFields.Any(f=>f.Id==g.FieldId));
        if(!string.IsNullOrWhiteSpace(input.Search)){var s=input.Search.Trim();q=q.Where(g=>g.Title.Contains(s)||g.Field.Name.Contains(s)||g.Field.City.Contains(s));}
        var total=await q.CountAsync();var page=Math.Min(input.Page,Math.Max(1,(int)Math.Ceiling(total/12d)));var ordered=input.Mode=="browse"?q.OrderBy(g=>g.StartsAt):q.OrderByDescending(g=>g.StartsAt);var ids=await ordered.ThenBy(g=>g.Id).Skip((page-1)*12).Take(12).Select(g=>g.Id).ToListAsync();var items=new List<object>();foreach(var id in ids)items.Add(await Details(actor,id));return new{Items=items,Total=total,Page=page,PageSize=12};
    }
    public async Task<object> Summary(int actor,bool owner)
    {
        var q=db.OpenGames.AsNoTracking().Where(g=>g.Status=="Scheduled"&&g.EndsAt>rules.Now);
        q=owner?q.Where(g=>g.OrganizerId==actor&&g.Field.OwnerId==actor):q.Where(g=>db.GameParticipants.Any(p=>p.GameId==g.Id&&p.UserId==actor&&p.Status=="Joined"));
        var total=await q.CountAsync();
        var participants=await db.GameParticipants.CountAsync(p=>p.Status=="Joined"&&q.Any(g=>g.Id==p.GameId));
        var byField=owner?await q.GroupBy(g=>g.FieldId).Select(g=>new{FieldId=g.Key,Count=g.Count()}).ToListAsync():null;
        var items=await q.OrderBy(g=>g.StartsAt).Take(12).Select(g=>new{g.Id,g.Title,g.FieldId,g.StartsAt,g.DurationMinutes}).ToListAsync();
        return new{Total=total,Participants=participants,ByField=byField,Items=items};
    }
}
