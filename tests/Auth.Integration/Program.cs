using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using KicksMvc.Data;
using KicksMvc.Models.Entities;
using KicksMvc.Services.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Cryptography;

TestDatabase.RequireIsolated();
if(args.Contains("--verify-test-isolation")){Console.WriteLine("PASS: explicit restored test database identity verified.");return;}
if(args.Contains("--stripe-fixture")){await StripeTests.Fixture(args[^1],args[^2]);return;}
if(args.Contains("--stripe"))
{
    try{await StripeTests.Run();}
    catch(Exception error){Console.Error.WriteLine(error is Stripe.StripeException?"FAIL: Official Stripe TEST API check failed; sensitive diagnostics suppressed.":Regex.Replace(error.Message,@"(?:sk|pk)_(?:test|live)_[A-Za-z0-9_]+|whsec_[A-Za-z0-9_]+","[REDACTED]"));Environment.ExitCode=1;}
    return;
}
if(args.Contains("--tournaments"))await TournamentTests.Run();
else if(args.Contains("--tournament-browser-setup"))await TournamentTests.Fixture(args[^1],"setup");
else if(args.Contains("--tournament-browser-cleanup"))await TournamentTests.Fixture(args[^1],"cleanup");
else if(args.Contains("--tournament-browser-ended"))await TournamentTests.Fixture(args[^1],"ended");
else if(args.Contains("--phase5"))await Phase5Tests.Run();
else if(args.Contains("--phase5-browser-setup"))await Phase5Tests.Fixture(args[^1],false);
else if(args.Contains("--phase5-browser-cleanup"))await Phase5Tests.Fixture(args[^1],true);
else if(args.Contains("--teams"))await TeamTests.Run();
else if(args.Contains("--teams-browser-setup"))await TeamTests.Fixture(args[^1],false);
else if(args.Contains("--teams-browser-cleanup"))await TeamTests.Fixture(args[^1],true);
else if(args.Contains("--bookings"))await BookingTests.RunAsync();
else if(args.Contains("--booking-browser-setup"))await BookingTests.FixtureAsync(args[^1],false);
else if(args.Contains("--booking-browser-cleanup"))await BookingTests.FixtureAsync(args[^1],true);
else if (args.Contains("--fields")) await FieldTests.RunAsync();
else if(args.Contains("--field-browser-setup"))await FieldTests.BrowserFixtureAsync(args[^1],false);
else if(args.Contains("--field-browser-cleanup"))await FieldTests.BrowserFixtureAsync(args[^1],true);
else if (args.Contains("--browser-bridge")) await BrowserBridge.RunAsync();
else await AuthTests.RunAsync();

static class AuthTests
{
    const string Password = "AuthTest2026!";
    const string Login = "/Pages/Auth/login.html", Register = "/Pages/Auth/register.html";
    static int passed;
    static void Check(bool value, string name)
    { if (!value) throw new Exception("FAIL: " + name); Console.WriteLine("PASS: " + name); passed++; }
    static string Token(string html) => WebUtility.HtmlDecode(Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
    static async Task<HttpResponseMessage> Post(HttpClient client, string url, Dictionary<string,string> values, bool proof = false, byte[]? proofBytes = null)
    {
        var html = await client.GetStringAsync(url.Contains("accounts/") ? "/Pages/Auth/accounts" : url);
        values["__RequestVerificationToken"] = Token(html);
        var content = new MultipartFormDataContent();
        foreach (var pair in values) content.Add(new StringContent(pair.Value), pair.Key);
        if(proof) { var file = new ByteArrayContent(proofBytes ?? "%PDF-1.7\n% isolated authentication test proof\n%%EOF"u8.ToArray()); file.Headers.ContentType = new("application/pdf"); content.Add(file, "OwnerProof", "auth-test.pdf"); }
        return await client.PostAsync(url, content);
    }
    static Dictionary<string,string> Registration(string email, string role = "player") => new()
    { ["FullName"] = "Isolated Auth Test", ["Email"] = email, ["Password"] = Password, ["ConfirmPassword"] = Password, ["Role"] = role };
    static Task<HttpResponseMessage> SignIn(HttpClient client, string email, string password = Password, string? returnTo = null) =>
        Post(client, Login, new() { ["Email"] = email, ["Password"] = password, ["ReturnTo"] = returnTo ?? "" });

    public static async Task RunAsync()
    {
        var marker = "auth-" + Guid.NewGuid().ToString("N");
        using var factory = new AuthFactory { AllowedPrefix = marker };
        using var visitor = factory.NewClient();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KicksDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var store = scope.ServiceProvider.GetRequiredService<OwnershipDocumentStore>();
        var createdIds = new List<int>(); var storedKeys = new List<string>();
        var before = await Snapshot(db);
        var administratorsBefore = await db.UserRoles.CountAsync(ur => db.Roles.Any(r => r.Id == ur.RoleId && r.Name == "admin"));
        var existingAdministratorId = await db.UserRoles.Where(ur => db.Roles.Any(r => r.Id == ur.RoleId && r.Name == "admin"))
            .Select(ur => ur.UserId).FirstOrDefaultAsync();
        try
        {
            Check(!await db.UserRoles.GroupBy(ur => ur.UserId).AnyAsync(group => group.Count() > 1), "existing accounts follow the one-global-role rule");
            if (existingAdministratorId != 0)
            {
                var realAdministrator = await db.Users.AsNoTracking().SingleAsync(user => user.Id == existingAdministratorId);
                Check(await scope.ServiceProvider.GetRequiredService<SignInManager<User>>().CanSignInAsync(realAdministrator), "existing real administrator remains eligible to sign in (read-only check)");
                var adminPrincipal = await scope.ServiceProvider.GetRequiredService<SignInManager<User>>().CreateUserPrincipalAsync(realAdministrator);
                Check(adminPrincipal.IsInRole("admin") && adminPrincipal.FindAll(ClaimTypes.Role).Count() == 1, "existing administrator principal derives exactly one admin role from Identity");
            }
            // Public routes render; each of the three protected dashboards challenges anonymous visitors.
            foreach (var route in new[]{"/index.html","/Pages/About/about.html","/Pages/Contact/contact.html","/Pages/Fields/fields.html","/Pages/Fields/field-details.html","/Pages/OpenGames/open-games.html","/Pages/OpenGames/game-details.html","/Pages/Teams/teams.html","/Pages/Challenges/challenges.html","/Pages/Tournaments/tournaments.html","/Pages/Tournaments/tournament-details.html","/Pages/Meetings/meeting.html",Login,Register,"/Pages/Demo/catalogue.html","/Pages/Demo/demo.html","/docs/database/database.html"})
            { using var response = await visitor.GetAsync(route);
              if(route=="/Pages/Teams/teams.html")Check(response.StatusCode==HttpStatusCode.Redirect&&response.Headers.Location!.ToString().Contains("login.html"),"Phase 4 Teams page requires an authenticated player");
              else Check(response.StatusCode == HttpStatusCode.OK, "MVC renders " + route); }
            foreach(var role in new[]{"player","owner","admin"})
            { using var response=await visitor.GetAsync($"/Pages/Dashboard/{role}.html"); Check(response.StatusCode==HttpStatusCode.Redirect && response.Headers.Location!.ToString().Contains("login.html"),"anonymous dashboard challenge: "+role); }
            using(var missingToken = await visitor.PostAsync(Register,new FormUrlEncodedContent(Registration(marker+"-csrf@example.test"))))
                Check(missingToken.StatusCode==HttpStatusCode.BadRequest,"registration requires anti-forgery token");
            using(var missingToken = await visitor.PostAsync(Login,new FormUrlEncodedContent(new Dictionary<string,string>{{"Email","unknown@example.test"},{"Password",Password}})))
                Check(missingToken.StatusCode==HttpStatusCode.BadRequest,"login requires anti-forgery token");
            foreach(var role in new[]{"admin","Admin","owner,admin"})
            { using var response=await Post(visitor,Register,Registration(marker+"-invalid@example.test",role)); Check(response.StatusCode==HttpStatusCode.OK && (await response.Content.ReadAsStringAsync()).Contains("Choose Player or Field owner"),"reject public role injection: "+role); }
            var weak=Registration(marker+"-invalid@example.test");weak["Password"]=weak["ConfirmPassword"]="12345678";
            using(var response=await Post(visitor,Register,weak))Check((await response.Content.ReadAsStringAsync()).Contains("including a letter"),"server password policy enforced");
            var invalid=Registration(marker+"-invalid@example.test");invalid["FullName"]=" ";invalid["ConfirmPassword"]="different";
            using(var response=await Post(visitor,Register,invalid))Check((await response.Content.ReadAsStringAsync()).Contains("validation-summary-errors"),"required name and password confirmation validated");
            using(var response=await Post(visitor,Register,Registration(marker+"-invalid@example.test","owner")))Check((await response.Content.ReadAsStringAsync()).Contains("Upload your field ownership proof"),"owner proof required on server");
            using(var response=await Post(visitor,Register,Registration(marker+"-invalid@example.test","owner"),proof:true,proofBytes:"<html>not a document</html>"u8.ToArray()))Check((await response.Content.ReadAsStringAsync()).Contains("matching file contents"),"spoofed owner proof MIME/extension rejected");
            using(var response=await Post(visitor,Register,Registration(marker+"-invalid@example.test","owner"),proof:true,proofBytes:new byte[1048577]))Check((await response.Content.ReadAsStringAsync()).Contains("up to 1 MB"),"oversized owner proof rejected");
            Check(!await db.Users.AnyAsync(u=>u.Email==marker+"-invalid@example.test"),"invalid requests create no users");

            string playerEmail=marker+"-player@example.test", ownerEmail=marker+"-owner@example.test";
            var playerInput=Registration(playerEmail);playerInput["Status"]="Active";playerInput["ReviewedBy"]="1";playerInput["Id"]="1";playerInput["RequestedRole"]="admin";
            using(var response=await Post(visitor,Register,playerInput))Check(response.StatusCode==HttpStatusCode.Redirect,"player registration succeeds");
            var player=await db.Users.SingleAsync(u=>u.Email==playerEmail);createdIds.Add(player.Id);
            Check(player.Status=="Pending" && player.RequestedRole=="player" && player.ReviewedBy==null && player.ReviewedAt==null && (await users.GetRolesAsync(player)).Count==0,"browser identity/status/approval injection ignored; player pending without role");
            Check(player.PasswordHash!=Password && users.PasswordHasher.VerifyHashedPassword(player,player.PasswordHash!,Password)!=PasswordVerificationResult.Failed,"SQL password uses verifiable Identity hash");
            using(var response=await Post(visitor,Register,Registration(playerEmail.ToUpperInvariant())))Check(response.StatusCode==HttpStatusCode.OK && (await response.Content.ReadAsStringAsync()).Contains("Unable to register"),"duplicate email blocked after normalization");
            using(var response=await Post(visitor,Register,Registration(ownerEmail,"owner"),proof:true))Check(response.StatusCode==HttpStatusCode.Redirect,"owner registration with proof succeeds");
            var owner=await db.Users.SingleAsync(u=>u.Email==ownerEmail);createdIds.Add(owner.Id);
            var proofRow=await db.OwnershipDocuments.SingleAsync(d=>d.UserId==owner.Id);storedKeys.Add(proofRow.StorageUrl);
            Check(owner.Status=="Pending" && owner.RequestedRole=="owner" && (await users.GetRolesAsync(owner)).Count==0 && File.Exists(store.PathFor(proofRow.StorageUrl)),"owner remains pending; proof metadata in SQL and bytes private");
            using(var response=await visitor.GetAsync("/App_Data/ownership/"+proofRow.StorageUrl))Check(response.StatusCode==HttpStatusCode.NotFound,"private proof cannot be downloaded as static file");
            using(var response=await SignIn(visitor,playerEmail))Check(response.StatusCode==HttpStatusCode.OK && (await response.Content.ReadAsStringAsync()).Contains("Unable to sign in"),"pending player cannot sign in");
            using(var response=await SignIn(visitor,ownerEmail))Check(response.StatusCode==HttpStatusCode.OK,"pending owner cannot sign in");

            // Only this in-memory TestServer can issue the synthetic administrator principal.
            // Existing administrator ID is read-only; only uniquely marked target accounts may be mutated.
            factory.ReviewerId=existingAdministratorId != 0 ? existingAdministratorId : player.Id;
            using var reviewer=factory.NewClient();reviewer.DefaultRequestHeaders.Add("X-Auth-Test-Reviewer",factory.ReviewerToken);
            using(var response=await reviewer.GetAsync("/Pages/Dashboard/admin.html"))Check(response.StatusCode==HttpStatusCode.OK,"admin dashboard renders under test-only reviewer principal");
            using(var response=await reviewer.PostAsync("/Pages/Auth/accounts/review",new StringContent("")))Check(response.StatusCode==HttpStatusCode.BadRequest,"account review requires anti-forgery token");
            using(var response=await reviewer.GetAsync("/Pages/Auth/accounts/documents/"+proofRow.Id))Check(response.StatusCode==HttpStatusCode.OK && response.Content.Headers.ContentDisposition?.DispositionType=="attachment","authorized reviewer downloads proof as attachment");
            using(var response=await Post(reviewer,"/Pages/Auth/accounts/review",new(){["Id"]=player.Id.ToString(),["ConcurrencyStamp"]=player.ConcurrencyStamp!,["Decision"]="approve",["AssignedRole"]="owner"}))Check(response.StatusCode==HttpStatusCode.Redirect,"owner approval without proof refused");
            await db.Entry(player).ReloadAsync();Check(player.Status=="Pending","failed approval leaves status unchanged");
            using(var response=await Post(reviewer,"/Pages/Auth/accounts/review",new(){["Id"]=player.Id.ToString(),["ConcurrencyStamp"]=player.ConcurrencyStamp!,["Decision"]="approve",["AssignedRole"]="admin"}))Check(response.StatusCode==HttpStatusCode.Redirect,"review cannot assign administrator role");
            foreach(var (user,role) in new[]{(player,"player"),(owner,"owner")})
            {
                using var response=await Post(reviewer,"/Pages/Auth/accounts/review",new(){["Id"]=user.Id.ToString(),["ConcurrencyStamp"]=user.ConcurrencyStamp!,["Decision"]="approve",["AssignedRole"]=role});
                await db.Entry(user).ReloadAsync();
                Check(response.StatusCode==HttpStatusCode.Redirect && user.Status=="Active" && user.ReviewedBy==factory.ReviewerId && user.ReviewedAt!=null && (await users.GetRolesAsync(user)).SequenceEqual(new[]{role}),"atomic approval and one Identity role: "+role);
            }
            using var playerClient=factory.NewClient();using var ownerClient=factory.NewClient();
            Check((await users.AddClaimAsync(player,new Claim(ClaimTypes.Role,"admin"))).Succeeded,"isolated role-shaped user claim fixture inserted");
            using(var response=await SignIn(playerClient,playerEmail,returnTo:"https://evil.example/"))Check(response.StatusCode==HttpStatusCode.Redirect && response.Headers.Location!.ToString()=="/index.html","valid player login rejects external return URL");
            using(var response=await playerClient.GetAsync("/Pages/Dashboard/admin.html"))Check(response.StatusCode==HttpStatusCode.Redirect && response.Headers.Location!.ToString().Contains("access-denied"),"custom role claims cannot override Identity role membership");
            using(var response=await SignIn(ownerClient,ownerEmail))Check(response.StatusCode==HttpStatusCode.Redirect && response.Headers.Location!.ToString()=="/Pages/Dashboard/owner.html","owner login redirects by Identity role");
            foreach(var (client,own,other) in new[]{(playerClient,"player","owner"),(ownerClient,"owner","player")})
            {
                using(var response=await client.GetAsync($"/Pages/Dashboard/{own}.html"))Check(response.StatusCode==HttpStatusCode.OK,"own dashboard allowed: "+own);
                using(var response=await client.GetAsync($"/Pages/Dashboard/{other}.html"))Check(response.StatusCode==HttpStatusCode.Redirect && response.Headers.Location!.ToString().Contains("access-denied"),"other role dashboard denied: "+own);
                using(var response=await client.GetAsync("/Pages/Auth/accounts"))Check(response.StatusCode==HttpStatusCode.Redirect,"SQL accounts restricted to administrator: "+own);
            }
            using(var response=await playerClient.PostAsync("/Pages/Auth/logout",new StringContent("")))Check(response.StatusCode==HttpStatusCode.BadRequest,"logout requires anti-forgery token");
            var home=await playerClient.GetStringAsync("/index.html");
            var stateJson=Regex.Match(home,"<script id=\"kicks-auth-state\" type=\"application/json\">(.*?)</script>",RegexOptions.Singleline).Groups[1].Value;
            Check(!home.Contains(player.PasswordHash!) && !home.Contains("security_stamp") && JsonDocument.Parse(stateJson).RootElement.GetProperty("profile").GetProperty("email").GetString()==playerEmail,"frontend receives profile but no credential/stamp fields");
            var token=JsonDocument.Parse(stateJson).RootElement.GetProperty("token").GetString()!;
            using(var response=await playerClient.PostAsync("/Pages/Auth/logout",new FormUrlEncodedContent(new Dictionary<string,string>{{"__RequestVerificationToken",token}})))Check(response.StatusCode==HttpStatusCode.Redirect,"logout clears cookie");
            using(var response=await playerClient.GetAsync("/Pages/Dashboard/player.html"))Check(response.StatusCode==HttpStatusCode.Redirect,"logged-out session denied");
            using(var response=await SignIn(playerClient,playerEmail,returnTo:"/Pages/Dashboard/player.html"))Check(response.Headers.Location!.ToString()=="/Pages/Dashboard/player.html","safe local return URL preserved");

            var staleStamp=owner.ConcurrencyStamp!;
            using(var response=await Post(reviewer,"/Pages/Auth/accounts/status",new(){["Id"]=owner.Id.ToString(),["ConcurrencyStamp"]=staleStamp,["Status"]="Inactive"}))Check(response.StatusCode==HttpStatusCode.Redirect,"administrator deactivates owner");
            await db.Entry(owner).ReloadAsync();Check(owner.Status=="Inactive","inactive persisted in SQL");
            using(var response=await ownerClient.GetAsync("/Pages/Dashboard/owner.html"))Check(response.StatusCode==HttpStatusCode.Redirect,"existing cookie revoked immediately on deactivation");
            using(var response=await SignIn(ownerClient,ownerEmail))Check(response.StatusCode==HttpStatusCode.OK,"inactive owner cannot sign in");
            using(var response=await Post(reviewer,"/Pages/Auth/accounts/status",new(){["Id"]=owner.Id.ToString(),["ConcurrencyStamp"]=staleStamp,["Status"]="Active"}))Check(response.StatusCode==HttpStatusCode.Redirect,"stale account command refused");
            await db.Entry(owner).ReloadAsync();Check(owner.Status=="Inactive","stale status command changed no data");
            using(var response=await Post(reviewer,"/Pages/Auth/accounts/status",new(){["Id"]=owner.Id.ToString(),["ConcurrencyStamp"]=owner.ConcurrencyStamp!,["Status"]="Active"}))Check(response.StatusCode==HttpStatusCode.Redirect,"administrator reactivates owner");
            using(var response=await SignIn(ownerClient,ownerEmail))Check(response.StatusCode==HttpStatusCode.Redirect,"reactivated owner can sign in");

            var rejectedEmail=marker+"-rejected@example.test";
            using(var response=await Post(visitor,Register,Registration(rejectedEmail)))Check(response.StatusCode==HttpStatusCode.Redirect,"isolated rejection request registered");
            var rejected=await db.Users.SingleAsync(u=>u.Email==rejectedEmail);createdIds.Add(rejected.Id);
            using(var response=await Post(reviewer,"/Pages/Auth/accounts/review",new(){["Id"]=rejected.Id.ToString(),["ConcurrencyStamp"]=rejected.ConcurrencyStamp!,["Decision"]="reject",["AssignedRole"]="player"}))Check(response.StatusCode==HttpStatusCode.Redirect,"review rejects request");
            using(var response=await SignIn(visitor,rejectedEmail))Check(response.StatusCode==HttpStatusCode.OK,"rejected account cannot sign in");
            await db.Entry(rejected).ReloadAsync();Check(rejected.Status=="Rejected" && (await users.GetRolesAsync(rejected)).Count==0,"rejection saves audit and assigns no role");
            for(var i=0;i<5;i++)using(await SignIn(visitor,playerEmail,"WrongPassword2026!")) { }
            await db.Entry(player).ReloadAsync();Check(await users.IsLockedOutAsync(player),"five invalid attempts lock account in SQL");
            using(var response=await SignIn(visitor,playerEmail))Check(response.StatusCode==HttpStatusCode.OK,"correct password cannot bypass lockout");
            using(var response=await playerClient.GetAsync("/Pages/Dashboard/player.html"))Check(response.StatusCode==HttpStatusCode.Redirect,"locked account existing cookie denied");
            Check(await db.UserRoles.CountAsync(ur=>db.Roles.Any(r=>r.Id==ur.RoleId && r.Name=="admin")) == administratorsBefore,"existing administrator membership preserved; no additional administrator created");
            var throttled=false;
            for(var i=0;i<31;i++)
            {
                using var response=await SignIn(visitor,marker+"-unknown@example.test");
                if(response.StatusCode==HttpStatusCode.TooManyRequests) { throttled=true; break; }
            }
            Check(throttled,"authentication POST throttle returns HTTP 429");
        }
        finally
        {
            // Discover only this run's uniquely marked records, even if a preceding assertion failed.
            var ids=await db.Users.Where(u=>u.Email!.StartsWith(marker)).Select(u=>u.Id).ToListAsync();
            storedKeys=await db.OwnershipDocuments.Where(d=>ids.Contains(d.UserId)).Select(d=>d.StorageUrl).ToListAsync();
            await using var transaction=await db.Database.BeginTransactionAsync();
            await db.UserRoles.Where(r=>ids.Contains(r.UserId)).ExecuteDeleteAsync();
            await db.UserClaims.Where(r=>ids.Contains(r.UserId)).ExecuteDeleteAsync();
            await db.UserLogins.Where(r=>ids.Contains(r.UserId)).ExecuteDeleteAsync();
            await db.UserTokens.Where(r=>ids.Contains(r.UserId)).ExecuteDeleteAsync();
            await db.OwnershipDocuments.Where(d=>ids.Contains(d.UserId)).ExecuteDeleteAsync();
            await db.Users.Where(u=>ids.Contains(u.Id)).ExecuteUpdateAsync(setters=>setters.SetProperty(u=>u.ReviewedBy,(int?)null));
            await db.Users.Where(u=>ids.Contains(u.Id)).ExecuteDeleteAsync();
            await transaction.CommitAsync();
            foreach(var key in storedKeys) store.Delete(key);
            Check(before==await Snapshot(db),"all table row data restored; only this run's test records removed");
        }
        Console.WriteLine($"Completed {passed} authentication checks.");
    }
    static async Task<string> Snapshot(KicksDbContext db)
    {
        // Content checksums and exact counts cover every physical table, including unrelated domain data.
        await db.Database.OpenConnectionAsync();
        var connection=db.Database.GetDbConnection();
        using var tables=connection.CreateCommand(); tables.CommandText="SELECT QUOTENAME(s.name)+'.'+QUOTENAME(t.name) FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id ORDER BY s.name,t.name";
        var names=new List<string>();using(var reader=await tables.ExecuteReaderAsync())while(await reader.ReadAsync())names.Add(reader.GetString(0));
        var results=new List<string>();
        foreach(var name in names){using var command=connection.CreateCommand();command.CommandText=$"SELECT COUNT_BIG(*), CHECKSUM_AGG(BINARY_CHECKSUM(*)) FROM {name}";using var reader=await command.ExecuteReaderAsync();await reader.ReadAsync();results.Add(name+":"+reader.GetValue(0)+":"+reader.GetValue(1));}
        return string.Join("|",results);
    }
}

sealed class AuthFactory : WebApplicationFactory<KicksMvc.Controllers.AuthController>
{
    public int ReviewerId {get;set;}
    public string ReviewerToken {get;set;} = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    public string AllowedPrefix {get;set;} = "auth-unconfigured-" + Guid.NewGuid().ToString("N");
    public string ContentRoot => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../"));
    public HttpClient NewClient()=>CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect=false });
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseContentRoot(ContentRoot).UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:DefaultConnection", TestDatabase.ConnectionString);
        builder.ConfigureLogging(logging=>logging.ClearProviders());
        builder.ConfigureTestServices(services=>
        {
            services.AddSingleton(this);
            services.AddScoped<TemporaryAccountGuard>();
            services.Configure<MvcOptions>(options => options.Filters.AddService<TemporaryAccountGuard>());
            services.AddAuthentication(options=>options.DefaultAuthenticateScheme="TestSwitch")
                .AddPolicyScheme("TestSwitch",null,options=>options.ForwardDefaultSelector=context=>
                    context.Request.Headers["X-Auth-Test-Reviewer"] == ReviewerToken || context.Request.Cookies["Kicks.TestReviewer"] == ReviewerToken
                    ? "TestReviewer":IdentityConstants.ApplicationScheme)
                .AddScheme<AuthenticationSchemeOptions,TestReviewerHandler>("TestReviewer",null);
        });
    }
}
// Test-only defense: the review principal cannot mutate any pre-existing real account.
sealed class TemporaryAccountGuard(KicksDbContext db, AuthFactory factory) : IAsyncResourceFilter
{
    public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
    {
        var request = context.HttpContext.Request;
        if (request.Method == "POST" && request.Path.StartsWithSegments("/Pages/Auth/accounts"))
        {
            var form = await request.ReadFormAsync();
            if (!int.TryParse(form["Id"], out var id) ||
                !await db.Users.AsNoTracking().AnyAsync(user => user.Id == id && user.Email!.StartsWith(factory.AllowedPrefix)))
            { context.Result = new StatusCodeResult(403); return; }
        }
        await next();
    }
}
sealed class TestReviewerHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, AuthFactory factory)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options,logger,encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync() => Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(
        new ClaimsPrincipal(new ClaimsIdentity(new[]{new Claim(ClaimTypes.NameIdentifier,factory.ReviewerId.ToString()),new Claim(ClaimTypes.Name,"Isolated test reviewer"),new Claim(ClaimTypes.Role,"admin")},Scheme.Name)),Scheme.Name)));
}

