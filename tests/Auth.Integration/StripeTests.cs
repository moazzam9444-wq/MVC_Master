using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text;
using System.Text.Json.Nodes;
using System.Security.Cryptography;
using KicksMvc.Data;
using KicksMvc.Models.Entities;
using KicksMvc.Services.Payments;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Stripe.Checkout;

static class StripeTests
{
    const string Password="TemporaryStripe2026!";
    public static async Task Fixture(string marker,string action)
    {
        if(!Regex.IsMatch(marker,"^stripe-(browser|sql)-[a-f0-9]{32}$"))throw new Exception("Unsafe Stripe fixture marker.");
        using var factory=new AuthFactory();using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<KicksDbContext>();
        if(action=="cleanup"){await Cleanup(db,marker);Console.WriteLine("Stripe fixtures cleaned.");return;}
        if(action=="inspect")
        {
            var ids=await db.Users.Where(u=>u.Email!.StartsWith(marker+"-")).Select(u=>u.Id).ToArrayAsync();
            Console.WriteLine(JsonSerializer.Serialize(new{bookings=await db.Bookings.Where(b=>ids.Contains(b.UserId)).OrderBy(b=>b.Id).Select(b=>new{b.Id,b.UserId,b.FieldId,b.Status,b.Price,b.CancellationReason,b.PaymentDeadline,b.StartsAt,b.EndsAt}).ToListAsync(),payments=await db.GatewayPayments.Where(p=>ids.Contains(p.PayerId)).OrderBy(p=>p.CreatedAt).Select(p=>new{p.Id,p.PayerId,p.BookingId,p.RegistrationId,p.Status,p.AmountJod,p.AmountUsdCents,p.UsdPerJod,p.FinancialReview,p.HoldExpiresAt,Attempts=p.Attempts.Count}).ToListAsync(),entries=await db.TournamentRegistrations.Where(r=>ids.Contains(r.Team.CaptainId)).OrderBy(r=>r.Id).Select(r=>new{r.Id,r.TournamentId,r.TeamId,r.Status}).ToListAsync()}));return;
        }
        if(action=="age")
        {
            var ids=await db.Users.Where(u=>u.Email!.StartsWith(marker+"-")).Select(u=>u.Id).ToArrayAsync();var p=await db.GatewayPayments.AsNoTracking().SingleAsync(p=>ids.Contains(p.PayerId)&&p.Status=="Pending");var expired=DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.AddMinutes(-1).ToUnixTimeSeconds());
            await using var tx=await db.Database.BeginTransactionAsync();await db.GatewayPayments.Where(g=>g.Id==p.Id).ExecuteUpdateAsync(s=>s.SetProperty(g=>g.HoldExpiresAt,expired));if(p.BookingId is int bid)await db.Bookings.Where(b=>b.Id==bid).ExecuteUpdateAsync(s=>s.SetProperty(b=>b.PaymentDeadline,expired));await tx.CommitAsync();Console.WriteLine("Only this fixture's unpaid hold aged; no payment success fabricated.");return;
        }
        if(action=="replay")
        {
            try
            {
                var ids=await db.Users.Where(u=>u.Email!.StartsWith(marker+"-")).Select(u=>u.Id).ToArrayAsync();var sessions=await db.StripeCheckoutAttempts.Where(a=>ids.Contains(a.Payment.PayerId)&&a.SessionId!=null).Select(a=>a.SessionId).ToArrayAsync();var settings=scope.ServiceProvider.GetRequiredService<SandboxSettings>();var client=settings.Client();var count=0;using var http=factory.NewClient();
                await foreach(var ev in client.V1.Events.ListAutoPagingAsync(new Stripe.EventListOptions{Limit=100,Types=["checkout.session.completed","checkout.session.expired"],Created=new Stripe.DateRangeOptions{GreaterThanOrEqual=DateTime.UtcNow.AddHours(-4)}}))
                {
                    if(ev.Data.Object is not Session session||!sessions.Contains(session.Id)||ev.Livemode)continue;
                    // Re-deliver actual provider events, with a fresh signature, to the real controller.
                    using var response=await Webhook(http,ev.ToJson(),settings.SigningSecret());if(!response.IsSuccessStatusCode)throw new Exception();count++;
                }
                Console.WriteLine(JsonSerializer.Serialize(new{replayed=count}));return;
            }catch{throw new Exception("Official Stripe TEST event replay failed; sensitive diagnostics suppressed.");}
        }
        if(action!="setup")throw new Exception("Invalid Stripe fixture action.");
        var users=scope.ServiceProvider.GetRequiredService<UserManager<User>>();var created=new List<User>();
        foreach(var role in new[]{"owner","other-owner","player","other-player","third-player"})
        {
            var u=new User{FullName="Temporary Stripe "+role,Email=marker+"-"+role+"@example.test",UserName=marker+"-"+role+"@example.test",Status="Active",LockoutEnabled=true};
            if(!(await users.CreateAsync(u,Password)).Succeeded||!(await users.AddToRoleAsync(u,role.Contains("owner")?"owner":"player")).Succeeded)throw new Exception("Fixture identity failed.");created.Add(u);
        }
        var f=new Field{OwnerId=created[0].Id,Name=marker+" paid pitch",City="Amman",Area="Isolated test",Description="Temporary Stripe sandbox fixture.",HourlyPrice=25.75m,Format="5v5"};
        var free=new Field{OwnerId=created[0].Id,Name=marker+" free pitch",City="Amman",Area="Isolated test",Description="Temporary free-flow fixture.",HourlyPrice=0,Format="5v5"};
        db.Fields.AddRange(f,free);await db.SaveChangesAsync();var teams=new List<Team>();
        for(var i=2;i<5;i++)
        {
            var t=new Team{CaptainId=created[i].Id,Name="Stripe test "+marker[^12..]+" "+i,City="Amman",Area="Test",Bio="Temporary sandbox team",UsualMatchday="Sunday",Initials="ST",Format="5v5"};db.Teams.Add(t);await db.SaveChangesAsync();db.TeamMembers.Add(new(){TeamId=t.Id,UserId=created[i].Id,Position="Midfielder"});teams.Add(t);
        }
        var start=DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(3)).Date.AddDays(5).AddHours(13);
        var tournament=new Tournament{CreatedBy=created[0].Id,FieldId=f.Id,Name=marker+" paid cup",StartsAt=new(start,TimeSpan.FromHours(3)),DurationMinutes=24*60*5+10*60,MatchDurationMinutes=60,RegistrationOpensAt=DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(3)).AddDays(-1),RegistrationClosesAt=new(start.AddHours(-1),TimeSpan.FromHours(3)),Capacity=2,EntryFee=7.50m,Prize="Test only",System="Single elimination",Level="All levels",CreationKey=Guid.NewGuid()};
        var freeCup=new Tournament{CreatedBy=created[0].Id,FieldId=f.Id,Name=marker+" free cup",StartsAt=tournament.StartsAt.AddDays(12),DurationMinutes=tournament.DurationMinutes,MatchDurationMinutes=60,RegistrationOpensAt=tournament.RegistrationOpensAt,RegistrationClosesAt=tournament.RegistrationClosesAt!.Value.AddDays(12),Capacity=4,EntryFee=0,Prize="Test",System="Single elimination",Level="All levels",CreationKey=Guid.NewGuid()};
        db.Tournaments.AddRange(tournament,freeCup);await db.SaveChangesAsync();
        Console.WriteLine(JsonSerializer.Serialize(new{fieldId=f.Id,freeFieldId=free.Id,tournamentId=tournament.Id,freeTournamentId=freeCup.Id,teamIds=teams.Select(t=>t.Id),playerIds=created.Skip(2).Select(u=>u.Id)}));
    }

    public static async Task Run()
    {
        var marker="stripe-sql-"+Guid.NewGuid().ToString("N");var passed=0;
        void Check(bool ok,string name){if(!ok)throw new Exception("FAIL: "+name);passed++;Console.WriteLine("PASS: "+name);}
        await Fixture(marker,"setup");using var factory=new AuthFactory();using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<KicksDbContext>();
        try
        {
            var settings=scope.ServiceProvider.GetRequiredService<SandboxSettings>();var client=settings.Client();
            var maintenance=scope.ServiceProvider.GetServices<Microsoft.Extensions.Hosting.IHostedService>().OfType<PaymentExpiryWorker>().Single();await maintenance.StopAsync(CancellationToken.None);
            Check(true,"isolated expiry tests run with background maintenance stopped");
            Check(settings.Convert(25.75m)==(1.40m,3605L),"decimal server conversion preserves JOD and exact USD cents");
            Check(settings.Convert(.36m).Cents==50,"fractional cents round away from zero to a USD cent");
            Check(settings.Convert(.39m).Cents==55,"fractional USD cents also round upward correctly");
            var cutoffDenied=false;try{scope.ServiceProvider.GetRequiredService<PaymentReservations>().Deadline(DateTimeOffset.UtcNow.AddMinutes(29).AddSeconds(59),1);}catch(KicksMvc.Services.Activities.ActivityException){cutoffDenied=true;}Check(cutoffDenied,"payment cannot start less than 30 minutes before its business cutoff");
            foreach(var amount in new[]{-1m,0m,.01m,9999999999.99m}){var rejected=false;try{settings.Convert(amount);}catch(KicksMvc.Services.Activities.ActivityException){rejected=true;}Check(rejected,"invalid/out-of-range payable amount rejected: "+amount);}
            foreach(var key in new[]{"Stripe:UsdPerJod","Stripe:SecretKey","Stripe:PublishableKey","Stripe:SandboxOnly"})
            {
                var configuration=new ConfigurationBuilder().AddConfiguration(scope.ServiceProvider.GetRequiredService<IConfiguration>()).AddInMemoryCollection(new Dictionary<string,string?>{{key,""}}).Build();var denied=false;try{new SandboxSettings(configuration).Rate();}catch(KicksMvc.Services.Activities.ActivityException){denied=true;}Check(denied,"missing safe sandbox setting fails closed: "+key);
            }
            var accounts=await db.Users.Where(u=>u.Email!.StartsWith(marker+"-")).ToDictionaryAsync(u=>u.Email!.Substring(marker.Length+1).Replace("@example.test",""));var field=await db.Fields.SingleAsync(f=>f.OwnerId==accounts["owner"].Id&&f.HourlyPrice>0);var free=await db.Fields.SingleAsync(f=>f.OwnerId==accounts["owner"].Id&&f.HourlyPrice==0);
            using var player=factory.NewClient();using var other=factory.NewClient();using var third=factory.NewClient();using var visitor=factory.NewClient();using var owner=factory.NewClient();
            foreach(var (http,role) in new[]{(player,"player"),(other,"other-player"),(third,"third-player"),(owner,"owner")}){using var response=await Post(http,"/Pages/Auth/login.html",new(){{"Email",accounts[role].Email!},{"Password",Password}});Check(response.StatusCode==HttpStatusCode.Redirect,"fixture Identity login: "+role);}
            var date=DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(3)).DateTime).AddDays(3);
            async Task<string> Quote(HttpClient http,int hour,int? fid=null)=> (await Json(await http.GetAsync($"/bookings/quote?FieldId={fid??field.Id}&Date={date:yyyy-MM-dd}&Hour={hour}&DurationMinutes=60"))).GetProperty("quoteToken").GetString()!;
            var quote=await Quote(player,13);using var created=await Post(player,"/bookings",new(){{"QuoteToken",quote},{"Price","0.01"},{"PlayerId",accounts["other-player"].Id.ToString()}});Check(created.StatusCode==HttpStatusCode.Created,"paid reservation creates an isolated temporary hold");var dto=await Json(created);var bookingId=dto.GetProperty("booking").GetProperty("id").GetInt32();var payment=await db.GatewayPayments.AsNoTracking().SingleAsync(p=>p.BookingId==bookingId);var url="/payments/"+payment.Id;
            Check(payment.PayerId==accounts["player"].Id&&payment.AmountJod==25.75m&&payment.AmountUsdCents==3605&&payment.UsdPerJod==1.4m,"SQL ignores forged payer/price and snapshots conversion");
            Check(payment.Status=="Pending"&&await db.Bookings.AnyAsync(b=>b.Id==bookingId&&b.Status=="Pending"),"unpaid reservation never becomes Confirmed");
            Check(payment.HoldExpiresAt-DateTimeOffset.UtcNow>TimeSpan.FromMinutes(29)&&payment.HoldExpiresAt-DateTimeOffset.UtcNow<=TimeSpan.FromMinutes(30),"hold deadline is 30 minutes on server time");
            using(var response=await Post(player,"/bookings",new(){{"QuoteToken",quote}}))Check(response.StatusCode==HttpStatusCode.OK,"duplicate hold submission returns durable replay");Check(await db.GatewayPayments.CountAsync(p=>p.BookingId==bookingId)==1,"one payment obligation per booking replay");
            using(var response=await other.GetAsync(url))Check(response.StatusCode==HttpStatusCode.NotFound,"another player cannot read payment");using(var response=await other.GetAsync("/api"+url))Check(response.StatusCode==HttpStatusCode.NotFound,"another player cannot poll payment");
            using(var response=await visitor.GetAsync(url))Check(response.StatusCode==HttpStatusCode.Redirect,"anonymous payment access denied");using(var response=await owner.GetAsync(url))Check(response.StatusCode==HttpStatusCode.Redirect,"owner cannot operate player checkout");
            using(var response=await Post(other,url+"/checkout"))Check(response.StatusCode==HttpStatusCode.NotFound,"another player cannot start checkout");using(var response=await Post(player,url+"/checkout",csrf:false))Check(response.StatusCode==HttpStatusCode.BadRequest,"checkout requires CSRF");using(var response=await Post(player,url+"/cancel",csrf:false))Check(response.StatusCode==HttpStatusCode.BadRequest,"hold cancellation requires CSRF");
            using(var response=await player.GetAsync(url+"?return=success"))Check(response.IsSuccessStatusCode&&await db.GatewayPayments.AnyAsync(p=>p.Id==payment.Id&&p.Status=="Pending"),"success redirect is never proof of payment");
            var csrf=await Token(player);var gate=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);async Task<HttpResponseMessage> Checkout(){await gate.Task;return await Post(player,url+"/checkout",new(){{"__RequestVerificationToken",csrf}},false);}var c1=Checkout();var c2=Checkout();gate.SetResult();var checkouts=await Task.WhenAll(c1,c2);
            Check(checkouts.All(r=>r.StatusCode==HttpStatusCode.Redirect),"simultaneous checkout requests both safely resume");Check(checkouts[0].Headers.Location==checkouts[1].Headers.Location,"concurrent requests use the same Stripe session");foreach(var r in checkouts)r.Dispose();var attempt=await db.StripeCheckoutAttempts.AsNoTracking().SingleAsync(a=>a.PaymentId==payment.Id);Check(attempt.SessionId is not null,"actual Stripe Checkout reference persists in SQL");
            var session=await client.V1.Checkout.Sessions.GetAsync(attempt.SessionId!);Check(!session.Livemode&&session.AmountTotal==3605&&session.Currency=="usd","official Stripe TEST session has exact server amount and currency");
            foreach(var http in new[]{player,owner,visitor}){using var denied=await http.GetAsync("/api/payments/history");Check(denied.StatusCode==HttpStatusCode.Redirect,"financial review history requires administrator authorization");}
            factory.ReviewerId=await db.UserRoles.Where(ur=>db.Roles.Any(r=>r.Id==ur.RoleId&&r.Name=="admin")).Select(ur=>ur.UserId).SingleAsync();using var reviewer=factory.NewClient();reviewer.DefaultRequestHeaders.Add("X-Auth-Test-Reviewer",factory.ReviewerToken);
            using(var response=await reviewer.GetAsync("/api/payments/history")){var history=await Json(response);Check(response.IsSuccessStatusCode&&history.GetProperty("items").EnumerateArray().Any(p=>p.GetProperty("id").GetGuid()==payment.Id),"isolated existing administrator principal reads actual SQL payment history");}
            using(var response=await reviewer.GetAsync("/payments/history"))Check(response.IsSuccessStatusCode&&(await response.Content.ReadAsStringAsync()).Contains("25.75 JOD"),"administrator financial review Razor renders SQL JOD/USD history");
            using(var response=await reviewer.GetAsync("/api/payments/history?page=0"))Check(response.StatusCode==HttpStatusCode.BadRequest,"payment history rejects invalid/unbounded page inputs");
            // Crash/timeout fixture: lose only the locally persisted response, then retry the same durable key.
            await db.StripeCheckoutAttempts.Where(a=>a.Id==attempt.Id).ExecuteUpdateAsync(s=>s.SetProperty(a=>a.SessionId,(string?)null).SetProperty(a=>a.Status,"Creating"));
            using(var retry=await Post(player,url+"/checkout"))Check(retry.StatusCode==HttpStatusCode.Redirect,"retry after a lost Stripe response safely resumes checkout");
            Check(await db.StripeCheckoutAttempts.AnyAsync(a=>a.Id==attempt.Id&&a.SessionId==session.Id)&&await db.StripeCheckoutAttempts.CountAsync(a=>a.PaymentId==payment.Id)==1,"Stripe idempotency recovers same session without another attempt");
            var eventId="evt_"+Guid.NewGuid().ToString("N");var eventBody=Envelope(session,eventId);var secret=settings.SigningSecret();
            using(var invalid=await Webhook(player,eventBody,"invalid"))Check(invalid.StatusCode==HttpStatusCode.BadRequest,"incorrect webhook signature rejected");Check(!await db.StripeWebhookEvents.AnyAsync(w=>w.EventId==eventId),"invalid signature creates no receipt or fulfilment");
            using(var forged=await Webhook(player,eventBody,secret))Check(forged.IsSuccessStatusCode,"validly signed but unpaid actual session handled safely");Check(await db.Bookings.AnyAsync(b=>b.Id==bookingId&&b.Status=="Pending"),"signed success-shaped event cannot override actual Stripe unpaid state");
            await Webhook(player,eventBody,secret);Check(await db.StripeWebhookEvents.CountAsync(w=>w.EventId==eventId)==1,"duplicate webhook is stored and processed once");
            foreach(var tamper in new[]{"amount_total","currency","client_reference_id","metadata"})
            {
                var json=JsonNode.Parse(Envelope(session,"evt_"+Guid.NewGuid().ToString("N")))!;var body=json["data"]!["object"]!;
                if(tamper=="amount_total")body[tamper]=3606;else if(tamper=="currency")body[tamper]="eur";else if(tamper=="metadata")body[tamper]!["kicks_payment"]=Guid.NewGuid().ToString("N");else body[tamper]="unrelated-reference";
                using var response=await Webhook(player,json.ToJsonString(),secret);Check(response.IsSuccessStatusCode&&await db.GatewayPayments.AnyAsync(p=>p.Id==payment.Id&&p.Status=="Pending"),"tampered "+tamper+" cannot fulfil payment");
            }
            using(var response=await Post(other,url+"/cancel"))Check(response.StatusCode==HttpStatusCode.NotFound,"foreign unpaid hold cancellation denied");using(var response=await Post(player,url+"/cancel"))Check(response.StatusCode==HttpStatusCode.Redirect,"payer cancels own unpaid hold");
            Check(await db.Bookings.AnyAsync(b=>b.Id==bookingId&&b.Status=="Cancelled")&&await db.GatewayPayments.AnyAsync(p=>p.Id==payment.Id&&p.Status=="Cancelled"),"cancellation keeps history and releases its hold");session=await client.V1.Checkout.Sessions.GetAsync(attempt.SessionId!);Check(session.Status=="expired","official Stripe API confirms canceled Checkout is expired");
            using(var response=await Post(player,url+"/checkout"))Check(response.StatusCode==HttpStatusCode.Conflict,"cancelled hold cannot create a new charge");using(var response=await other.GetAsync($"/bookings/quote?FieldId={field.Id}&Date={date:yyyy-MM-dd}&Hour=13"))Check(response.IsSuccessStatusCode,"cancelled paid hold releases SQL availability");
            var fq=await Quote(player,13,free.Id);using(var response=await Post(player,"/bookings",new(){{"QuoteToken",fq}})){var json=await Json(response);Check(response.StatusCode==HttpStatusCode.Created&&json.GetProperty("booking").GetProperty("status").GetString()=="Confirmed"&&json.GetProperty("paymentUrl").ValueKind==JsonValueKind.Null,"free reservation remains immediate and has no payment");}
            var q1=await Quote(player,15);var q2=await Quote(other,15);var token1=await Token(player);var token2=await Token(other);var start=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);async Task<HttpResponseMessage> Race(HttpClient http,string q,string t){await start.Task;return await Post(http,"/bookings",new(){{"QuoteToken",q},{"__RequestVerificationToken",t}},false);}var a1=Race(player,q1,token1);var a2=Race(other,q2,token2);start.SetResult();var races=await Task.WhenAll(a1,a2);Check(races.Count(r=>r.StatusCode==HttpStatusCode.Created)==1&&races.Count(r=>r.StatusCode==HttpStatusCode.Conflict)==1,"concurrent paid reservations cannot both hold overlapping time");foreach(var r in races)r.Dispose();
            var held=await db.GatewayPayments.AsNoTracking().SingleAsync(p=>p.Status=="Pending"&&p.Booking!=null&&p.Booking.FieldId==field.Id);var expired=DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.AddMinutes(-1).ToUnixTimeSeconds());
            // Explicit isolated expiry fixture; never fabricates payment success or changes original data.
            await db.GatewayPayments.Where(p=>p.Id==held.Id).ExecuteUpdateAsync(s=>s.SetProperty(p=>p.HoldExpiresAt,expired));await db.Bookings.Where(b=>b.Id==held.BookingId).ExecuteUpdateAsync(s=>s.SetProperty(b=>b.PaymentDeadline,expired));
            using(var response=await other.GetAsync($"/bookings/quote?FieldId={field.Id}&Date={date:yyyy-MM-dd}&Hour=15"))Check(response.IsSuccessStatusCode,"expired hold releases SQL availability even before maintenance");
            var rebook=await Quote(other,15);using(var response=await Post(other,"/bookings",new(){{"QuoteToken",rebook}}))Check(response.StatusCode==HttpStatusCode.Created,"new reservation at exactly the expired start succeeds before maintenance; unique index preserved");
            await scope.ServiceProvider.GetRequiredService<StripePaymentService>().Sweep();Check(await db.Bookings.AnyAsync(b=>b.Id==held.BookingId&&b.Status=="Cancelled"&&b.CancellationReason=="Expired")&&await db.GatewayPayments.AnyAsync(p=>p.Id==held.Id&&p.Status=="Expired"),"maintenance persists expiry without deleting history");
            var cup=await db.Tournaments.SingleAsync(t=>t.CreatedBy==accounts["owner"].Id&&t.EntryFee>0);using(var response=await Post(player,$"/tournaments/{cup.Id}/register",new(){{"EntryFee","0"},{"TeamId","999999"}}))Check(response.StatusCode==HttpStatusCode.Created,"paid tournament registration creates held place");var entry=await db.TournamentRegistrations.AsNoTracking().SingleAsync(r=>r.TournamentId==cup.Id);var fee=await db.GatewayPayments.AsNoTracking().SingleAsync(p=>p.RegistrationId==entry.Id);Check(entry.Status=="Pending"&&fee.AmountJod==7.50m&&fee.AmountUsdCents==1050&&fee.PayerId==accounts["player"].Id,"tournament place remains Pending at SQL-derived fee and payer");
            using(var response=await Post(player,$"/tournaments/{cup.Id}/register"))Check(response.StatusCode==HttpStatusCode.OK,"duplicate registration resumes existing held place");Check(await db.GatewayPayments.CountAsync(p=>p.RegistrationId==entry.Id)==1,"duplicate entry creates no second payment obligation");
            using(var response=await Post(player,$"/tournaments/{cup.Id}/withdraw"))Check(response.IsSuccessStatusCode,"captain withdraws pending tournament entry");Check(await db.GatewayPayments.AnyAsync(p=>p.Id==fee.Id&&p.Status=="Cancelled"),"withdrawal cancels unpaid tournament checkout");
            using(var response=await Post(player,$"/tournaments/{cup.Id}/register"))Check(response.StatusCode==HttpStatusCode.Created,"withdrawn unpaid entry may create a fresh payment hold");
            var seatGate=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);var ot=await Token(other);var tt=await Token(third);async Task<HttpResponseMessage> Seat(HttpClient http,string token){await seatGate.Task;return await Post(http,$"/tournaments/{cup.Id}/register",new(){{"__RequestVerificationToken",token}},false);}var sr1=Seat(other,ot);var sr2=Seat(third,tt);seatGate.SetResult();var seats=await Task.WhenAll(sr1,sr2);
            Check(seats.Count(r=>r.StatusCode==HttpStatusCode.Created)==1&&seats.Count(r=>r.StatusCode==HttpStatusCode.Conflict)==1,"simultaneous paid registrations cannot oversubscribe the final held place");var winner=seats[0].StatusCode==HttpStatusCode.Created?other:third;foreach(var r in seats)r.Dispose();
            Check(await db.TournamentRegistrations.CountAsync(r=>r.TournamentId==cup.Id&&r.Status=="Pending")==2,"capacity includes both unpaid tournament holds");await Post(winner,$"/tournaments/{cup.Id}/withdraw");await Post(player,$"/tournaments/{cup.Id}/withdraw");
            await Post(player,$"/tournaments/{cup.Id}/register");var aged=await db.GatewayPayments.AsNoTracking().SingleAsync(p=>p.RegistrationId==entry.Id&&p.Status=="Pending");await db.GatewayPayments.Where(p=>p.Id==aged.Id).ExecuteUpdateAsync(s=>s.SetProperty(p=>p.HoldExpiresAt,expired));await db.TournamentRegistrations.Where(r=>r.Id==entry.Id).ExecuteUpdateAsync(s=>s.SetProperty(r=>r.PaymentDeadline,expired));
            using(var response=await Post(other,$"/tournaments/{cup.Id}/register"))Check(response.StatusCode==HttpStatusCode.Created,"expired paid tournament hold no longer consumes a place without a worker");using(var response=await Post(third,$"/tournaments/{cup.Id}/register"))Check(response.StatusCode==HttpStatusCode.Created,"both released capacity places remain usable");
            var freeCup=await db.Tournaments.SingleAsync(t=>t.CreatedBy==accounts["owner"].Id&&t.EntryFee==0);using(var response=await Post(player,$"/tournaments/{freeCup.Id}/register"))Check(response.StatusCode==HttpStatusCode.Created,"free tournament entry remains immediate");Check(await db.TournamentRegistrations.AnyAsync(r=>r.TournamentId==freeCup.Id&&r.Status=="Registered")&&!await db.GatewayPayments.AnyAsync(p=>p.Registration!=null&&p.Registration.TournamentId==freeCup.Id),"free tournament has no gateway payment");
            Check(!await db.Payments.AnyAsync(p=>p.Booking!=null&&p.Booking.FieldId==field.Id),"legacy demo payments are never fabricated");
            Console.WriteLine("STRIPE SQL PASS: "+passed);
            var evidence=Path.Combine(factory.ContentRoot,".local/stripe-phase/stripe-sql-result.json");await File.WriteAllTextAsync(evidence,JsonSerializer.Serialize(new{passed,failed=0}));
        }
        finally{await Cleanup(db,marker);}
    }
    static async Task<string> Token(HttpClient http){var html=await http.GetStringAsync("/index.html");return JsonDocument.Parse(Regex.Match(html,"<script id=\"kicks-auth-state\" type=\"application/json\">(.*?)</script>",RegexOptions.Singleline).Groups[1].Value).RootElement.GetProperty("token").GetString()!;}
    static async Task<HttpResponseMessage> Post(HttpClient http,string url,Dictionary<string,string>? values=null,bool csrf=true){values??=[];if(csrf)values["__RequestVerificationToken"]=await Token(http);return await http.PostAsync(url,new FormUrlEncodedContent(values));}
    static async Task<JsonElement> Json(HttpResponseMessage response)=>JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();
    static string Envelope(Session session,string id)=>new JsonObject{["id"]=id,["object"]="event",["type"]="checkout.session.completed",["livemode"]=false,["api_version"]=Stripe.StripeConfiguration.ApiVersion,["data"]=new JsonObject{["object"]=JsonNode.Parse(session.ToJson())}}.ToJsonString();
    static async Task<HttpResponseMessage> Webhook(HttpClient http,string json,string secret)
    {
        var time=DateTimeOffset.UtcNow.ToUnixTimeSeconds();var signature=Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret),Encoding.UTF8.GetBytes(time+"."+json))).ToLowerInvariant();using var request=new HttpRequestMessage(HttpMethod.Post,"/payments/stripe/webhook"){Content=new StringContent(json,Encoding.UTF8,"application/json")};request.Headers.Add("Stripe-Signature","t="+time+",v1="+signature);return await http.SendAsync(request);
    }
    static async Task Cleanup(KicksDbContext db,string marker)
    {
        db.ChangeTracker.Clear();var users=await db.Users.Where(u=>u.Email!.StartsWith(marker+"-")).Select(u=>u.Id).ToArrayAsync();
        var fields=await db.Fields.Where(f=>users.Contains(f.OwnerId)).Select(f=>f.Id).ToArrayAsync();var teams=await db.Teams.Where(t=>users.Contains(t.CaptainId)).Select(t=>t.Id).ToArrayAsync();var tournaments=await db.Tournaments.Where(t=>fields.Contains(t.FieldId)&&users.Contains(t.CreatedBy)).Select(t=>t.Id).ToArrayAsync();var entries=await db.TournamentRegistrations.Where(r=>tournaments.Contains(r.TournamentId)).Select(r=>r.Id).ToArrayAsync();var bookings=await db.Bookings.Where(b=>fields.Contains(b.FieldId)&&users.Contains(b.UserId)).Select(b=>b.Id).ToArrayAsync();
        var payments=await db.GatewayPayments.Where(p=>users.Contains(p.PayerId)&&(bookings.Contains(p.BookingId??0)||entries.Contains(p.RegistrationId??0))).Select(p=>p.Id).ToArrayAsync();
        // Provider event history is intentionally retained; only explicitly bound fixture records are removed.
        await using var tx=await db.Database.BeginTransactionAsync();
        await db.StripeCheckoutAttempts.Where(a=>payments.Contains(a.PaymentId)).ExecuteDeleteAsync();await db.GatewayPayments.Where(p=>payments.Contains(p.Id)).ExecuteDeleteAsync();
        await db.TournamentMatchSlots.Where(s=>tournaments.Contains(s.TournamentId)).ExecuteDeleteAsync();await db.TournamentMatches.Where(m=>tournaments.Contains(m.TournamentId)).ExecuteDeleteAsync();await db.TournamentDrawEntries.Where(e=>tournaments.Contains(e.TournamentId)).ExecuteDeleteAsync();await db.RegistrationPlayers.Where(p=>entries.Contains(p.RegistrationId)).ExecuteDeleteAsync();await db.TournamentRegistrations.Where(r=>entries.Contains(r.Id)).ExecuteDeleteAsync();await db.Tournaments.Where(t=>tournaments.Contains(t.Id)).ExecuteDeleteAsync();
        var challenges=await db.Challenges.Where(c=>teams.Contains(c.HostTeamId)).Select(c=>c.Id).ToArrayAsync();await db.BookingTeams.Where(t=>bookings.Contains(t.BookingId)).ExecuteDeleteAsync();await db.Bookings.Where(b=>bookings.Contains(b.Id)).ExecuteDeleteAsync();await db.ChallengeRequests.Where(r=>challenges.Contains(r.ChallengeId)).ExecuteDeleteAsync();await db.Challenges.Where(c=>challenges.Contains(c.Id)).ExecuteDeleteAsync();
        await db.TeamRequests.Where(r=>teams.Contains(r.TeamId)).ExecuteDeleteAsync();await db.TeamRecruitingPositions.Where(p=>teams.Contains(p.TeamId)).ExecuteDeleteAsync();await db.TeamMembers.Where(m=>teams.Contains(m.TeamId)).ExecuteDeleteAsync();await db.Teams.Where(t=>teams.Contains(t.Id)).ExecuteDeleteAsync();await db.FieldAmenities.Where(a=>fields.Contains(a.FieldId)).ExecuteDeleteAsync();await db.FieldImages.Where(i=>fields.Contains(i.FieldId)).ExecuteDeleteAsync();await db.Fields.Where(f=>fields.Contains(f.Id)).ExecuteDeleteAsync();await db.UserRoles.Where(r=>users.Contains(r.UserId)).ExecuteDeleteAsync();await db.UserClaims.Where(r=>users.Contains(r.UserId)).ExecuteDeleteAsync();await db.UserLogins.Where(r=>users.Contains(r.UserId)).ExecuteDeleteAsync();await db.UserTokens.Where(r=>users.Contains(r.UserId)).ExecuteDeleteAsync();await db.Users.Where(u=>users.Contains(u.Id)).ExecuteDeleteAsync();await tx.CommitAsync();
    }
}
