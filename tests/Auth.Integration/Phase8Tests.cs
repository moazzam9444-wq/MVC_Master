using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using KicksMvc.Data;
using KicksMvc.Models.Entities;
using KicksMvc.Services.Workspace;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

static class Phase8Tests
{
    const string Password="TemporaryPhase8Test2026!";
    static int passed;
    static void Check(bool ok,string text){if(!ok)throw new Exception("FAIL: "+text);passed++;Console.WriteLine("PASS: "+text);}
    static async Task<string> Token(HttpClient c){var html=await c.GetStringAsync("/index.html");return JsonDocument.Parse(Regex.Match(html,"<script id=\"kicks-auth-state\" type=\"application/json\">(.*?)</script>",RegexOptions.Singleline).Groups[1].Value).RootElement.GetProperty("token").GetString()!;}
    static async Task<HttpResponseMessage> Post(HttpClient c,string url,Dictionary<string,string> body,bool csrf=true){if(csrf)body["__RequestVerificationToken"]=await Token(c);return await c.PostAsync(url,new FormUrlEncodedContent(body));}
    static async Task Login(HttpClient c,string email){using var r=await Post(c,"/Pages/Auth/login.html",new(){{"Email",email},{"Password",Password}});Check(r.StatusCode==HttpStatusCode.Redirect,"Identity login for isolated Phase 8 actor");}
    public static async Task Run()
    {
        var marker="phase8-test-"+Guid.NewGuid().ToString("N");using var factory=new AuthFactory();using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<KicksDbContext>();var users=scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var ids=new List<int>();int fieldId=0;var originalSetting=await db.Settings.AsNoTracking().SingleOrDefaultAsync(s=>s.Id==1);bool settingsTouched=false;
        try
        {
            async Task<User> Actor(string role,string suffix){var u=new User{FullName=marker+" "+suffix,Email=marker+"-"+suffix+"@example.test",UserName=marker+"-"+suffix+"@example.test",Status="Active",LockoutEnabled=true};Check((await users.CreateAsync(u,Password)).Succeeded,"temporary account created");ids.Add(u.Id);Check((await users.AddToRoleAsync(u,role)).Succeeded,"temporary role assigned");return u;}
            var owner=await Actor("owner","owner");var player=await Actor("player","player");var other=await Actor("player","other");
            var field=new Field{OwnerId=owner.Id,Name=marker+" <img src=x onerror=alert(1)>",City="Amman",Area="Test",Description="Isolated QA fixture",Format="5v5",HourlyPrice=0};db.Fields.Add(field);await db.SaveChangesAsync();fieldId=field.Id;
            for(int i=0;i<26;i++)db.Bookings.Add(new Booking{FieldId=field.Id,OwnerId=owner.Id,UserId=player.Id,StartsAt=new DateTimeOffset(DateTime.SpecifyKind(DateTime.UtcNow.Date.AddDays(i+2).AddHours(13),DateTimeKind.Unspecified),TimeSpan.FromHours(3)),Price=0,OwnerNet=0,Status="Confirmed"});await db.SaveChangesAsync();
            using var visitor=factory.NewClient();using var p=factory.NewClient();using var o=factory.NewClient();using var stranger=factory.NewClient();using var admin=factory.NewClient();
            factory.ReviewerId=await db.UserRoles.Where(ur=>db.Roles.Any(r=>r.Id==ur.RoleId&&r.Name=="admin")).Select(ur=>ur.UserId).SingleAsync();admin.DefaultRequestHeaders.Add("X-Auth-Test-Reviewer",factory.ReviewerToken);
            await Login(p,player.Email!);await Login(o,owner.Email!);await Login(stranger,other.Email!);
            using(var r=await visitor.GetAsync("/dashboard/overview"))Check(r.StatusCode==HttpStatusCode.Redirect,"anonymous overview denied");
            using(var r=await visitor.GetAsync("/error"))Check(r.StatusCode==HttpStatusCode.InternalServerError,"branded error page preserves HTTP 500");
            using(var r=await Post(visitor,"/error",new(),false))Check(r.StatusCode==HttpStatusCode.BadRequest,"POST error endpoint retains global anti-forgery protection");
            using(var r=await Post(visitor,"/error",new()))Check(r.StatusCode==HttpStatusCode.InternalServerError,"error recovery accepts the original POST method without a 405");
            using(var r=await p.GetAsync("/dashboard/records/bookings"))Check(r.StatusCode==HttpStatusCode.Redirect,"player cannot read platform booking ledger");
            using(var r=await p.GetAsync("/dashboard/records/fields"))Check(r.StatusCode==HttpStatusCode.Redirect,"player cannot read administrator field review");
            Check((await admin.GetStringAsync("/dashboard/records/fields?search="+marker)).Contains("&lt;img"),"administrator reviews actual SQL fields with encoded names");
            var service=scope.ServiceProvider.GetRequiredService<WorkspaceService>();var own=await service.Overview(player.Id,"player");Check(own.Metrics.Single(m=>m.Label=="Bookings").Value==26,"overview SQL count includes records beyond the first page");Check(own.Bookings.Count==5&&own.BookingStatuses.Single(m=>m.Label=="Confirmed").Value==26,"bounded recent rows and complete status aggregates");
            Check((await service.Overview(other.Id,"player")).Bookings.Count==0,"unrelated player overview contains no booking data");Check((await service.Overview(owner.Id,"owner")).Metrics.Single(m=>m.Label=="Bookings").Value==26,"owner overview is scoped to owned fields");
            var overview=await p.GetStringAsync("/dashboard/overview");Check(overview.Contains("&lt;img")&&!overview.Contains("<img src=x"),"SQL stored titles are Razor encoded");Check(!overview.Contains(owner.Email!)&&!overview.Contains(owner.PasswordHash!),"player overview omits unrelated emails and credentials");
            var ledger=await admin.GetStringAsync("/dashboard/records/bookings?search="+marker);Check(Regex.Matches(ledger,"data-label=\"Field\"").Count==20,"administrator ledger first page bounded to twenty");var second=await admin.GetStringAsync("/dashboard/records/bookings?page=2&search="+marker);Check(Regex.Matches(second,"data-label=\"Field\"").Count==6,"administrator ledger second page contains remaining six");
            var contacts=await p.GetStringAsync("/api/messages/contacts");Check(contacts.Contains(owner.FullName)&&!contacts.Contains(other.FullName),"SQL booking relationship grants contact access only to eligible accounts");Check(!contacts.Contains("password")&&!contacts.Contains("securityStamp")&&!contacts.Contains(owner.Email!),"contact projection excludes sensitive account fields");
            using(var r=await Post(p,"/messages",new(){{"RecipientId",owner.Id.ToString()},{"Body","CSRF test"}},false))Check(r.StatusCode==HttpStatusCode.BadRequest,"message requires anti-forgery token");
            using(var r=await Post(p,"/messages",new(){{"RecipientId",other.Id.ToString()},{"Body","forbidden"}}))Check(r.StatusCode==HttpStatusCode.NotFound,"unrelated recipient IDOR rejected");
            using(var r=await Post(p,"/messages",new(){{"RecipientId",owner.Id.ToString()},{"SenderId",other.Id.ToString()},{"Body","<script>qa</script> "+marker}}))Check(r.IsSuccessStatusCode,"eligible SQL message saved");
            var message=await db.Messages.AsNoTracking().SingleAsync(m=>m.SenderId==player.Id&&m.RecipientId==owner.Id);Check(message.SenderId==player.Id&&message.Body.StartsWith("<script>"),"server assigns sender and preserves text for encoded rendering");
            using(var r=await stranger.GetAsync("/api/messages/"+owner.Id))Check(r.StatusCode==HttpStatusCode.NotFound,"third party cannot read another conversation");
            var thread=await o.GetStringAsync("/api/messages/"+player.Id);Check(thread.Contains(marker),"recipient can reload persisted SQL conversation");
            using(var r=await Post(o,"/messages/"+player.Id+"/read",new()))Check(r.IsSuccessStatusCode,"recipient marks own conversation read");Check(await db.Messages.AnyAsync(m=>m.Id==message.Id&&m.ReadAt!=null),"read receipt persisted");
            using(var r=await Post(p,"/messages",new(){{"RecipientId",owner.Id.ToString()},{"Body",new string('x',2001)}}))Check(r.StatusCode==HttpStatusCode.BadRequest,"server rejects oversized message");
            using(var r=await Post(p,"/messages",new(){{"RecipientId",player.Id.ToString()},{"Body","self"}}))Check(r.StatusCode==HttpStatusCode.NotFound,"self messaging rejected");
            for(int i=0;i<51;i++)db.Messages.Add(new Message{SenderId=player.Id,RecipientId=owner.Id,Body="History "+i});await db.SaveChangesAsync();
            var history=JsonDocument.Parse(await o.GetStringAsync("/api/messages/"+player.Id)).RootElement;
            Check(history.GetProperty("items").GetArrayLength()==50&&history.GetProperty("hasOlder").GetBoolean(),"conversation history is bounded to fifty messages");
            var oldest=history.GetProperty("items")[0].GetProperty("id").GetInt32();var older=JsonDocument.Parse(await o.GetStringAsync("/api/messages/"+player.Id+"?before="+oldest)).RootElement;
            Check(older.GetProperty("items").GetArrayLength()==2,"older history is accessible without truncating conversation");
            owner.Status="Inactive";await users.UpdateAsync(owner);
            using(var r=await Post(p,"/messages",new(){{"RecipientId",owner.Id.ToString()},{"Body","inactive"}}))Check(r.StatusCode==HttpStatusCode.NotFound,"inactive recipient cannot receive new messages");
            using(var r=await p.GetAsync("/api/messages/"+owner.Id))Check(r.StatusCode==HttpStatusCode.NotFound,"inactive contact conversation is not exposed");
            using(var r=await Post(visitor,"/feedback",new(){{"Name",marker},{"Email",player.Email!},{"Rating","5"},{"Message","Isolated quality feedback"}},false))Check(r.StatusCode==HttpStatusCode.BadRequest,"public feedback still requires CSRF");
            using(var r=await Post(visitor,"/feedback",new(){{"Name",marker},{"Email","invalid"},{"Rating","9"},{"Message","short"}}))Check(r.StatusCode==HttpStatusCode.BadRequest,"feedback validates email, rating and length on server");
            using(var r=await Post(visitor,"/feedback",new(){{"Name",marker},{"Email",player.Email!},{"Rating","5"},{"Message","<script>qa feedback</script>"}}))Check(r.IsSuccessStatusCode,"public feedback saved to SQL");
            Check(await db.Feedback.AnyAsync(f=>f.Name==marker),"feedback survives a fresh SQL query");using(var r=await p.GetAsync("/dashboard/records/feedback"))Check(r.StatusCode==HttpStatusCode.Redirect,"feedback review is administrator only");
            var feedback=await admin.GetStringAsync("/dashboard/records/feedback?search="+marker);Check(feedback.Contains("&lt;script&gt;")&&!feedback.Contains("<script>qa"),"administrator feedback is encoded");
            using(var r=await p.GetAsync("/dashboard/settings"))Check(r.StatusCode==HttpStatusCode.Redirect,"site presentation restricted to administrator");using(var r=await Post(p,"/dashboard/settings",new(){{"Title","forged"}}))Check(r.StatusCode==HttpStatusCode.Redirect,"player cannot mutate site settings");
            var presentation=await visitor.GetStringAsync("/api/site-presentation");Check(!presentation.Contains("commissionRate")&&!presentation.Contains("listingFee"),"public presentation excludes legacy financial settings");
            settingsTouched=true;
            using(var r=await Post(admin,"/dashboard/settings",new(){{"Title",marker},{"Description","Isolated site presentation"},{"City","Amman"},{"Contact","QA contact"},{"CommissionRate","99"},{"ListingFee","999"}}))Check(r.IsSuccessStatusCode,"administrator saves presentation through validated server endpoint");
            var savedSetting=await db.Settings.AsNoTracking().SingleAsync(s=>s.Id==1);
            Check(savedSetting.Title==marker&&savedSetting.CommissionRate==(originalSetting?.CommissionRate??10m)&&savedSetting.ListingFee==(originalSetting?.ListingFee??5m),"presentation saves ignore forged financial fields and preserve payment policy");
            Check((await visitor.GetStringAsync("/api/site-presentation")).Contains(marker),"public presentation reads saved SQL values");
            using(var r=await Post(admin,"/dashboard/settings",new(){{"Title",new string('x',201)},{"Description","valid"},{"City","Amman"}}))Check(r.StatusCode==HttpStatusCode.BadRequest,"site presentation has server length validation");
            Check(!(await visitor.GetStringAsync("/js/admin-account.js")).Contains("passwordHash"),"public legacy administrator verifier removed");
            using(var r=await visitor.GetAsync("/App_Data/ownership/invalid.bin"))Check(r.StatusCode==HttpStatusCode.NotFound,"private files remain unavailable to static middleware");
        }
        finally
        {
            db.ChangeTracker.Clear();
            if(settingsTouched){if(originalSetting is null)await db.Settings.Where(s=>s.Id==1).ExecuteDeleteAsync();else{db.Settings.Update(originalSetting);await db.SaveChangesAsync();db.ChangeTracker.Clear();}}
            await db.Messages.Where(m=>ids.Contains(m.SenderId)||ids.Contains(m.RecipientId)).ExecuteDeleteAsync();await db.Feedback.Where(f=>f.Name==marker).ExecuteDeleteAsync();await db.Bookings.Where(b=>b.FieldId==fieldId).ExecuteDeleteAsync();await db.Fields.Where(f=>f.Id==fieldId&&f.Name.StartsWith(marker)).ExecuteDeleteAsync();await db.UserRoles.Where(r=>ids.Contains(r.UserId)).ExecuteDeleteAsync();await db.Users.Where(u=>ids.Contains(u.Id)&&u.Email!.StartsWith(marker)).ExecuteDeleteAsync();
            Check(!await db.Users.AnyAsync(u=>u.Email!.StartsWith(marker)),"all Phase 8 fixture accounts removed from isolated database");
        }
        Console.WriteLine($"Completed {passed} Phase 8 SQL assertions.");
    }
}
