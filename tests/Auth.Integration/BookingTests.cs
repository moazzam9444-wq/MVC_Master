using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using KicksMvc.Data;
using KicksMvc.Models.Entities;
using KicksMvc.Services.Bookings;
using KicksMvc.Services.Fields;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

static class BookingTests
{
    const string Password="TemporaryBooking2026!";static int passed;
    static void Check(bool value,string name){if(!value)throw new Exception("FAIL: "+name);passed++;Console.WriteLine("PASS: "+name);}
    static async Task<JsonElement> Json(HttpResponseMessage response)=>JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();
    static async Task<string> Token(HttpClient client){var html=await client.GetStringAsync("/index.html");var match=Regex.Match(html,"<script id=\"kicks-auth-state\" type=\"application/json\">(.*?)</script>",RegexOptions.Singleline);return JsonDocument.Parse(match.Groups[1].Value).RootElement.GetProperty("token").GetString()!;}
    static async Task Login(HttpClient client,string email){using var result=await client.PostAsync("/Pages/Auth/login.html",new FormUrlEncodedContent(new Dictionary<string,string>{{"Email",email},{"Password",Password},{"__RequestVerificationToken",await Token(client)}}));Check(result.StatusCode==HttpStatusCode.Redirect,"fixture signs in through real Identity");}
    static async Task<HttpResponseMessage> Post(HttpClient client,string url,Dictionary<string,string> input,bool csrf=true){if(csrf)input["__RequestVerificationToken"]=await Token(client);return await client.PostAsync(url,new FormUrlEncodedContent(input));}
    static async Task<HttpResponseMessage> Quote(HttpClient client,int fieldId,DateOnly date,int hour,int duration=60)=>await client.GetAsync($"/bookings/quote?FieldId={fieldId}&Date={date:yyyy-MM-dd}&Hour={hour}&DurationMinutes={duration}");
    static async Task<string> QuoteToken(HttpClient client,int id,DateOnly date,int hour){using var response=await Quote(client,id,date,hour);if(!response.IsSuccessStatusCode)throw new Exception("Quote failed: "+await response.Content.ReadAsStringAsync());return(await Json(response)).GetProperty("quoteToken").GetString()!;}
    static async Task<User> User(UserManager<User> users,string marker,string suffix,string role){var account=new User{FullName="Temporary booking "+suffix,Email=marker+"-"+suffix+"@example.test",UserName=marker+"-"+suffix+"@example.test",Status="Active",LockoutEnabled=true};if(!(await users.CreateAsync(account,Password)).Succeeded||!(await users.AddToRoleAsync(account,role)).Succeeded)throw new Exception("Fixture creation failed");return account;}
    static async Task<(User Owner,User OtherOwner,User Player,User OtherPlayer,int FieldId)> Setup(KicksDbContext db,UserManager<User> users,string marker)
    {var owner=await User(users,marker,"owner","owner");var other=await User(users,marker,"other-owner","owner");var player=await User(users,marker,"player","player");var second=await User(users,marker,"other-player","player");var field=new Field{OwnerId=owner.Id,Name=marker+" pitch",City="Amman",Area="Test district",Description="Temporary booking verification pitch.",HourlyPrice=25.75m,Format="5v5"};db.Fields.Add(field);await db.SaveChangesAsync();return(owner,other,player,second,field.Id);}
    public static async Task FixtureAsync(string marker,bool cleanup)
    {
        if(!Regex.IsMatch(marker,"^booking-browser-[a-f0-9]{32}$"))throw new Exception("Invalid fixture marker");using var factory=new AuthFactory();using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<KicksDbContext>();
        if(cleanup){await Cleanup(db,marker);Console.WriteLine("Booking browser fixtures removed.");return;}
        var data=await Setup(db,scope.ServiceProvider.GetRequiredService<UserManager<User>>(),marker);
        var ended=new Booking{FieldId=data.FieldId,OwnerId=data.Owner.Id,UserId=data.Player.Id,StartsAt=new DateTimeOffset(DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(3)).Date.AddDays(-1).AddHours(10),TimeSpan.FromHours(3)),Price=25.75m,OwnerNet=25.75m,Status="Confirmed"};db.Bookings.Add(ended);await db.SaveChangesAsync();Console.WriteLine(JsonSerializer.Serialize(new{fieldId=data.FieldId,endedBookingId=ended.Id}));
    }
    public static async Task RunAsync()
    {
        var marker="booking-test-"+Guid.NewGuid().ToString("N");var clock=new BookingClock();
        using var initial=new AuthFactory();using var factory=initial.WithWebHostBuilder(builder=>builder.ConfigureTestServices(services=>services.AddScoped(provider=>new BookingService(provider.GetRequiredService<KicksDbContext>(),provider.GetRequiredService<FieldService>(),provider.GetRequiredService<IDataProtectionProvider>(),clock))));
        using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<KicksDbContext>();var users=scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        try{
            var data=await Setup(db,users,marker);using var player=factory.CreateClient(new(){AllowAutoRedirect=false});using var second=factory.CreateClient(new(){AllowAutoRedirect=false});using var owner=factory.CreateClient(new(){AllowAutoRedirect=false});using var otherOwner=factory.CreateClient(new(){AllowAutoRedirect=false});using var visitor=factory.CreateClient(new(){AllowAutoRedirect=false});
            await Login(player,data.Player.Email!);await Login(second,data.OtherPlayer.Email!);await Login(owner,data.Owner.Email!);await Login(otherOwner,data.OtherOwner.Email!);
            var day=DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(3)).DateTime).AddDays(2);var yesterday=day.AddDays(-3);
            using(var response=await visitor.GetAsync($"/api/fields/{data.FieldId}/availability?date={day:yyyy-MM-dd}")){var available=await Json(response);Check(response.IsSuccessStatusCode&&available.GetProperty("slots").GetArrayLength()==13,"availability has thirteen hourly starts from 10 to 22");Check(available.GetProperty("durationMinutes").GetInt32()==60,"duration is the schema-supported one hour");}
            using(var response=await Quote(player,data.FieldId,yesterday,12))Check(response.StatusCode==HttpStatusCode.BadRequest,"past reservations rejected");
            using(var response=await Quote(player,data.FieldId,day,12,90))Check(response.StatusCode==HttpStatusCode.BadRequest,"unsupported duration rejected");
            using(var response=await Quote(player,data.FieldId,day,9))Check(response.StatusCode==HttpStatusCode.BadRequest,"out-of-hours slot rejected");
            using(var response=await Quote(player,int.MaxValue,day,12))Check(response.StatusCode==HttpStatusCode.Conflict,"missing field rejected");
            using(var response=await Quote(owner,data.FieldId,day,12))Check(response.StatusCode==HttpStatusCode.Redirect,"owner cannot use player quote endpoint");
            using(var response=await Quote(visitor,data.FieldId,day,12))Check(response.StatusCode==HttpStatusCode.Redirect,"anonymous player reservation denied");
            var quote=await QuoteToken(player,data.FieldId,day,10);
            using(var response=await Post(player,"/bookings",new(){{"QuoteToken",quote}},false))Check(response.StatusCode==HttpStatusCode.BadRequest,"booking creation requires CSRF token");
            using(var response=await Post(visitor,"/bookings",new(){{"QuoteToken",quote}}))Check(response.StatusCode==HttpStatusCode.Redirect,"anonymous creation denied");
            using(var response=await Post(second,"/bookings",new(){{"QuoteToken",quote}}))Check(response.StatusCode==HttpStatusCode.BadRequest,"another player's quote cannot be reused");
            using var responseCreated=await Post(player,"/bookings",new(){{"QuoteToken",quote},{"Price","0.01"},{"PlayerId",data.OtherPlayer.Id.ToString()},{"OwnerId",data.OtherOwner.Id.ToString()},{"Status","Paid"}});
            Check(responseCreated.StatusCode==HttpStatusCode.Created,"active player creates a confirmed reservation");var booking=(await Json(responseCreated)).GetProperty("booking");var id=booking.GetProperty("id").GetInt32();
            using(var freshScope=scope.ServiceProvider.CreateScope()){var persisted=await freshScope.ServiceProvider.GetRequiredService<KicksDbContext>().Bookings.SingleAsync(b=>b.Id==id);Check(persisted.UserId==data.Player.Id&&persisted.OwnerId==data.Owner.Id&&persisted.FieldId==data.FieldId,"authoritative player/owner/field associations persist in a fresh context");Check(persisted.Price==25.75m&&persisted.Commission==0&&persisted.CommissionRate==0&&persisted.OwnerNet==25.75m,"price snapshot ignores forged values and settlement is deferred");Check(persisted.EndsAt-persisted.StartsAt==TimeSpan.FromHours(1)&&persisted.StartsAt.Offset==TimeSpan.FromHours(3),"SQL computed end and Amman offset preserved");Check(persisted.Status=="Confirmed"&&!persisted.IsSplit,"no client-assigned paid/split status");}
            Check(!await db.Payments.AnyAsync(p=>p.BookingId==id)&&!await db.BookingShares.AnyAsync(s=>s.BookingId==id)&&!await db.BookingTeams.AnyAsync(t=>t.BookingId==id),"no fake payment, shares or teams created");
            using(var replay=await Post(player,"/bookings",new(){{"QuoteToken",quote}}))Check(replay.StatusCode==HttpStatusCode.OK&&(await Json(replay)).GetProperty("booking").GetProperty("id").GetInt32()==id,"duplicate confirmation returns original persisted booking");
            Check(await db.Bookings.CountAsync(b=>b.FieldId==data.FieldId&&b.UserId==data.Player.Id&&b.StartsAt==booking.GetProperty("startsAt").GetDateTimeOffset())==1,"duplicate submission creates no extra row");
            using(var overlap=await Quote(second,data.FieldId,day,10))Check(overlap.StatusCode==HttpStatusCode.Conflict,"occupied slot rejected at quote time");
            using(var availability=await visitor.GetAsync($"/api/fields/{data.FieldId}/availability?date={day:yyyy-MM-dd}"))Check(!(await Json(availability)).GetProperty("slots")[0].GetProperty("available").GetBoolean(),"new booking removes SQL availability");
            var adjacent=await QuoteToken(second,data.FieldId,day,11);using(var adjacentResult=await Post(second,"/bookings",new(){{"QuoteToken",adjacent}}))Check(adjacentResult.StatusCode==HttpStatusCode.Created,"adjacent half-open reservation succeeds");
            using(var result=await second.GetAsync("/bookings/"+id))Check(result.StatusCode==HttpStatusCode.NotFound,"player A cannot read player B reservation");
            using(var result=await otherOwner.GetAsync("/bookings/"+id))Check(result.StatusCode==HttpStatusCode.NotFound,"owner A cannot read owner B reservation");
            Check((await player.GetStringAsync("/bookings")).Contains("25.75")&&(await owner.GetStringAsync("/bookings")).Contains(data.Player.FullName),"player and field owner lists show persisted reservation");
            using(var result=await Post(second,$"/bookings/{id}/cancel",new(){{"Version",booking.GetProperty("version").GetString()!}}))Check(result.StatusCode==HttpStatusCode.NotFound,"foreign player cancellation denied");
            using(var result=await Post(otherOwner,$"/bookings/{id}/cancel",new(){{"Version",booking.GetProperty("version").GetString()!}}))Check(result.StatusCode==HttpStatusCode.NotFound,"foreign owner cancellation denied");
            using(var result=await Post(owner,$"/bookings/{id}/complete",new(){{"Version",booking.GetProperty("version").GetString()!}}))Check(result.StatusCode==HttpStatusCode.Conflict,"cannot complete a future reservation");
            using(var result=await Post(player,$"/bookings/{id}/complete",new(){{"Version",booking.GetProperty("version").GetString()!}}))Check(result.StatusCode==HttpStatusCode.Redirect,"players cannot complete reservations");
            using(var result=await Post(player,$"/bookings/{id}/cancel",new(){{"Version",booking.GetProperty("version").GetString()!}},false))Check(result.StatusCode==HttpStatusCode.BadRequest,"cancellation requires anti-forgery token");
            var pending=new Booking{FieldId=data.FieldId,OwnerId=data.Owner.Id,UserId=data.Player.Id,StartsAt=new DateTimeOffset(day.ToDateTime(new TimeOnly(15,0)),TimeSpan.FromHours(3)),Price=25.75m,OwnerNet=25.75m,Status="Pending"};db.Bookings.Add(pending);await db.SaveChangesAsync();
            using(var blocked=await Quote(second,data.FieldId,day,15))Check(blocked.StatusCode==HttpStatusCode.Conflict,"existing Pending reservations also block availability");
            var pendingDto=await Json(await player.GetAsync("/bookings/"+pending.Id));using(var result=await Post(player,$"/bookings/{pending.Id}/cancel",new(){{"Version",pendingDto.GetProperty("version").GetString()!}}))Check(result.StatusCode==HttpStatusCode.Conflict,"players cannot cancel Pending reservations");
            using(var result=await Post(owner,$"/bookings/{pending.Id}/cancel",new(){{"Version",pendingDto.GetProperty("version").GetString()!}}))Check(result.IsSuccessStatusCode,"owner may cancel future Pending reservation without changing payment records");
            var game=new OpenGame{FieldId=data.FieldId,OrganizerId=data.Owner.Id,Title="Temporary occupancy fixture",StartsAt=new DateTimeOffset(day.ToDateTime(new TimeOnly(20,30)),TimeSpan.FromHours(3)),DurationMinutes=30,Capacity=10,PlayerFee=0,Level="All levels",Status="Scheduled"};db.OpenGames.Add(game);await db.SaveChangesAsync();
            using(var blocked=await Quote(player,data.FieldId,day,20))Check(blocked.StatusCode==HttpStatusCode.Conflict,"existing SQL activity interval is respected without implementing its business module");
            using(var adjacentEvent=await Quote(player,data.FieldId,day,21))Check(adjacentEvent.IsSuccessStatusCode,"activity ending exactly at slot start permits adjacency");
            // Real simultaneous HTTP requests, separate authenticated players and SQL contexts.
            var firstQuote=await QuoteToken(player,data.FieldId,day,14);var secondQuote=await QuoteToken(second,data.FieldId,day,14);var csrf1=await Token(player);var csrf2=await Token(second);var gate=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            async Task<HttpResponseMessage> Race(HttpClient client,string q,string csrf){await gate.Task;return await client.PostAsync("/bookings",new FormUrlEncodedContent(new Dictionary<string,string>{{"QuoteToken",q},{"__RequestVerificationToken",csrf}}));}
            var r1=Race(player,firstQuote,csrf1);var r2=Race(second,secondQuote,csrf2);gate.SetResult();var race=await Task.WhenAll(r1,r2);Check(race.Count(r=>r.StatusCode==HttpStatusCode.Created)==1&&race.Count(r=>r.StatusCode==HttpStatusCode.Conflict)==1,"simultaneous conflicting players produce exactly one success and one HTTP 409");foreach(var r in race)r.Dispose();Check(await db.Bookings.CountAsync(b=>b.FieldId==data.FieldId&&b.StartsAt.Hour==14&&b.Status=="Confirmed")==1,"SQL stores at most one concurrent reservation");
            var priceQuote=await QuoteToken(player,data.FieldId,day,16);await db.Fields.Where(f=>f.Id==data.FieldId).ExecuteUpdateAsync(set=>set.SetProperty(f=>f.HourlyPrice,30m));using(var changed=await Post(player,"/bookings",new(){{"QuoteToken",priceQuote}}))Check(changed.StatusCode==HttpStatusCode.Conflict,"price changes require a fresh explicit quote");
            var beforeArchive=await QuoteToken(player,data.FieldId,day,16);
            await db.Fields.Where(f=>f.Id==data.FieldId).ExecuteUpdateAsync(set=>set.SetProperty(f=>f.IsArchived,true));using(var archived=await Quote(player,data.FieldId,day,16))Check(archived.StatusCode==HttpStatusCode.Conflict,"archived field cannot be reserved");await db.Fields.Where(f=>f.Id==data.FieldId).ExecuteUpdateAsync(set=>set.SetProperty(f=>f.IsArchived,false));
            await db.Fields.Where(f=>f.Id==data.FieldId).ExecuteUpdateAsync(set=>set.SetProperty(f=>f.IsArchived,true));using(var archivedCreate=await Post(player,"/bookings",new(){{"QuoteToken",beforeArchive}}))Check(archivedCreate.StatusCode==HttpStatusCode.Conflict,"creation rechecks archival after quote issuance");await db.Fields.Where(f=>f.Id==data.FieldId).ExecuteUpdateAsync(set=>set.SetProperty(f=>f.IsArchived,false));
            data.Owner.Status="Inactive";await users.UpdateAsync(data.Owner);using(var inactive=await Quote(player,data.FieldId,day,16))Check(inactive.StatusCode==HttpStatusCode.Conflict,"inactive-owner field cannot be reserved");using(var inactiveCreate=await Post(player,"/bookings",new(){{"QuoteToken",beforeArchive}}))Check(inactiveCreate.StatusCode==HttpStatusCode.Conflict,"creation rechecks owner activity after quote issuance");data.Owner.Status="Active";await users.UpdateAsync(data.Owner);
            // Exact three-hour cutoff: fake time applies only to BookingService, never Identity cookies.
            var cutoffQuote=await QuoteToken(player,data.FieldId,day,18);var cutoffBooking=(await Json(await Post(player,"/bookings",new(){{"QuoteToken",cutoffQuote}}))).GetProperty("booking");var cutoffId=cutoffBooking.GetProperty("id").GetInt32();var start=cutoffBooking.GetProperty("startsAt").GetDateTimeOffset();clock.Value=start.AddHours(-3).AddSeconds(1);
            using(var expired=await Post(player,"/bookings",new(){{"QuoteToken",beforeArchive}}))Check(expired.StatusCode==HttpStatusCode.Conflict,"expired unused quote cannot create a reservation");
            using(var close=await Post(player,$"/bookings/{cutoffId}/cancel",new(){{"Version",cutoffBooking.GetProperty("version").GetString()!}}))Check(close.StatusCode==HttpStatusCode.Conflict&&(await close.Content.ReadAsStringAsync()).Contains("at least 3 hours"),"player cancellation with less than three hours is rejected clearly");
            clock.Value=start.AddHours(-3);using(var exact=await Post(player,$"/bookings/{cutoffId}/cancel",new(){{"Version",cutoffBooking.GetProperty("version").GetString()!}}))Check(exact.IsSuccessStatusCode,"player cancellation succeeds at exactly three hours using server time");
            using(var stale=await Post(owner,$"/bookings/{cutoffId}/cancel",new(){{"Version",cutoffBooking.GetProperty("version").GetString()!}}))Check(stale.StatusCode==HttpStatusCode.Conflict,"stale status command cannot overwrite cancellation");
            using(var replay=await Post(player,"/bookings",new(){{"QuoteToken",cutoffQuote}}))Check((await Json(replay)).GetProperty("booking").GetProperty("status").GetString()=="Cancelled","replaying old confirmation after cancellation cannot rebook");
            using(var result=await visitor.GetAsync($"/api/fields/{data.FieldId}/availability?date={day:yyyy-MM-dd}"))Check((await Json(result)).GetProperty("slots")[8].GetProperty("available").GetBoolean(),"cancelled booking releases its slot");
            var freshQuote=await QuoteToken(player,data.FieldId,day,18);var fresh=(await Json(await Post(player,"/bookings",new(){{"QuoteToken",freshQuote}}))).GetProperty("booking");Check(fresh.GetProperty("id").GetInt32()!=cutoffId,"a fresh explicit quote can reserve a cancelled slot anew");clock.Value=start.AddMinutes(-30);
            using(var result=await Post(owner,$"/bookings/{fresh.GetProperty("id").GetInt32()}/cancel",new(){{"Version",fresh.GetProperty("version").GetString()!}}))Check(result.IsSuccessStatusCode,"owner can cancel future own-field booking within three hours");
            Check(await db.Bookings.AnyAsync(b=>b.Id==cutoffId&&b.Status=="Cancelled"&&b.CancelledBy==data.Player.Id&&b.CancelledAt!=null),"cancelled row and actor audit retained in SQL");
            var ended=new Booking{FieldId=data.FieldId,UserId=data.Player.Id,OwnerId=data.Owner.Id,StartsAt=start.AddDays(-1),Price=30,OwnerNet=30,Status="Confirmed"};db.Bookings.Add(ended);await db.SaveChangesAsync();var endedDto=await Json(await owner.GetAsync("/bookings/"+ended.Id));
            using(var result=await Post(owner,$"/bookings/{ended.Id}/complete",new(){{"Version",endedDto.GetProperty("version").GetString()!}}))Check(result.IsSuccessStatusCode,"owner completes ended confirmed session");endedDto=await Json(await owner.GetAsync("/bookings/"+ended.Id));using(var result=await Post(owner,$"/bookings/{ended.Id}/cancel",new(){{"Version",endedDto.GetProperty("version").GetString()!}}))Check(result.StatusCode==HttpStatusCode.Conflict,"completed reservation cannot be cancelled");
            using(var response=await player.GetAsync("/bookings?Period=history"))Check((await response.Content.ReadAsStringAsync()).Contains("Cancelled"),"history retains cancelled sessions");
            using(var response=await player.GetAsync("/bookings/data?Page=0"))Check(response.StatusCode==HttpStatusCode.BadRequest,"unbounded invalid paging rejected");
            for(var n=0;n<14;n++)db.Bookings.Add(new Booking{FieldId=data.FieldId,OwnerId=data.Owner.Id,UserId=data.Player.Id,StartsAt=start.AddDays(n+5),Price=30,OwnerNet=30,Status="Confirmed"});await db.SaveChangesAsync();
            using(var response=await player.GetAsync("/bookings/data?Page=1")){var list=await Json(response);Check(list.GetProperty("items").GetArrayLength()==12&&list.GetProperty("total").GetInt32()==await db.Bookings.CountAsync(b=>b.UserId==data.Player.Id),"booking queries are bounded and totals are player-scoped");}
            using(var response=await player.GetAsync("/bookings/data?Page=2"))Check((await Json(response)).GetProperty("items").GetArrayLength()>0,"second booking page returns remaining history");
            Check((await owner.GetStringAsync("/owner/fields")).Contains(marker),"existing SQL field management remains available");
        }finally{await Cleanup(db,marker);Check(!await db.Users.AnyAsync(u=>u.Email!.StartsWith(marker)),"only booking-run temporary fixtures removed");}
        Console.WriteLine($"Completed {passed} booking integration assertions.");
    }
    static async Task Cleanup(KicksDbContext db,string marker)
    {
        db.ChangeTracker.Clear();var users=await db.Users.Where(u=>u.Email!.StartsWith(marker+"-")).Select(u=>u.Id).ToListAsync();var fields=await db.Fields.Where(f=>users.Contains(f.OwnerId)).Select(f=>f.Id).ToListAsync();await using var tx=await db.Database.BeginTransactionAsync();
        await db.Bookings.Where(b=>fields.Contains(b.FieldId)&&users.Contains(b.UserId)).ExecuteDeleteAsync();await db.OpenGames.Where(g=>fields.Contains(g.FieldId)&&users.Contains(g.OrganizerId)).ExecuteDeleteAsync();await db.FieldAmenities.Where(a=>fields.Contains(a.FieldId)).ExecuteDeleteAsync();await db.FieldImages.Where(i=>fields.Contains(i.FieldId)).ExecuteDeleteAsync();await db.Fields.Where(f=>fields.Contains(f.Id)).ExecuteDeleteAsync();await db.UserRoles.Where(u=>users.Contains(u.UserId)).ExecuteDeleteAsync();await db.UserClaims.Where(u=>users.Contains(u.UserId)).ExecuteDeleteAsync();await db.UserLogins.Where(u=>users.Contains(u.UserId)).ExecuteDeleteAsync();await db.UserTokens.Where(u=>users.Contains(u.UserId)).ExecuteDeleteAsync();await db.Users.Where(u=>users.Contains(u.Id)).ExecuteDeleteAsync();await tx.CommitAsync();
    }
    private sealed class BookingClock:TimeProvider { public DateTimeOffset Value {get;set;}=DateTimeOffset.UtcNow;public override DateTimeOffset GetUtcNow()=>Value.ToUniversalTime(); }
}


