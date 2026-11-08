using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using KicksMvc.Data;
using KicksMvc.Models.Entities;
using KicksMvc.Services.Fields;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

static class FieldTests
{
    const string Password="TemporaryField2026!";
    static int passed;
    static readonly byte[] Png=Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jE1sAAAAASUVORK5CYII=");
    static void Check(bool value,string name){if(!value)throw new Exception("FAIL: "+name);passed++;Console.WriteLine("PASS: "+name);}
    static async Task<string> Token(HttpClient client){var html=await client.GetStringAsync("/index.html");var input=Regex.Match(html,"name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");if(input.Success)return WebUtility.HtmlDecode(input.Groups[1].Value);var state=Regex.Match(html,"<script id=\"kicks-auth-state\" type=\"application/json\">(.*?)</script>",RegexOptions.Singleline);return JsonDocument.Parse(state.Groups[1].Value).RootElement.GetProperty("token").GetString()!;}
    static async Task Login(HttpClient client,string email){var content=new FormUrlEncodedContent(new Dictionary<string,string>{{"Email",email},{"Password",Password},{"__RequestVerificationToken",await Token(client)}});using var response=await client.PostAsync("/Pages/Auth/login.html",content);Check(response.StatusCode==HttpStatusCode.Redirect,"temporary "+email.Split('@')[0].Split('-')[^1]+" signs in with Identity");}
    static async Task<HttpResponseMessage> Post(HttpClient client,string url,Dictionary<string,string>? changes=null,byte[]? file=null,string mime="image/png",string name="pitch.png",bool csrf=true)
    {
        var data=new Dictionary<string,string>{{"Name","Temporary field"},{"City","Amman"},{"Area","Test district"},{"Description","An isolated temporary field for verification."},{"Format","5v5"},{"HourlyPrice","25.50"}};
        if(changes!=null)foreach(var change in changes)data[change.Key]=change.Value;
        using var content=new MultipartFormDataContent();foreach(var item in data)content.Add(new StringContent(item.Value),item.Key);
        if(csrf)content.Add(new StringContent(await Token(client)),"__RequestVerificationToken");
        if(file!=null){var upload=new ByteArrayContent(file);upload.Headers.ContentType=new(mime);content.Add(upload,"Photo",name);}
        return await client.PostAsync(url,content);
    }
    static async Task<JsonElement> Json(HttpResponseMessage response)=>JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();
    static async Task<User> Create(UserManager<User> users,string marker,string role,string suffix)
    {var user=new User{FullName="Temporary field "+suffix,UserName=marker+"-"+suffix+"@example.test",Email=marker+"-"+suffix+"@example.test",Status="Active",LockoutEnabled=true};var result=await users.CreateAsync(user,Password);if(!result.Succeeded)throw new Exception("Fixture creation failed");result=await users.AddToRoleAsync(user,role);if(!result.Succeeded)throw new Exception("Fixture role failed");return user;}
    public static async Task BrowserFixtureAsync(string marker,bool cleanup)
    {
        if(!Regex.IsMatch(marker,"^field-browser-[a-f0-9]{32}$"))throw new Exception("Invalid fixture marker");
        using var factory=new AuthFactory();using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<KicksDbContext>();
        if(cleanup){await Cleanup(db,scope.ServiceProvider.GetRequiredService<FieldImageStore>(),marker);Console.WriteLine("Browser fixtures removed.");return;}
        var users=scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        await Create(users,marker,"owner","owner");await Create(users,marker,"owner","other");await Create(users,marker,"player","player");Console.WriteLine("Temporary browser owner/player fixtures ready.");
    }
    public static async Task RunAsync()
    {
        var marker="field-test-"+Guid.NewGuid().ToString("N");using var factory=new AuthFactory();using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<KicksDbContext>();var users=scope.ServiceProvider.GetRequiredService<UserManager<User>>();var imageStore=scope.ServiceProvider.GetRequiredService<FieldImageStore>();
        try{
            var a=await Create(users,marker,"owner","owner-a");var b=await Create(users,marker,"owner","owner-b");var p=await Create(users,marker,"player","player");
            using var owner=factory.NewClient();using var other=factory.NewClient();using var player=factory.NewClient();using var visitor=factory.NewClient();await Login(owner,a.Email!);await Login(other,b.Email!);await Login(player,p.Email!);
            foreach(var client in new[]{visitor,player}){using var response=await client.GetAsync("/owner/fields");Check(response.StatusCode==HttpStatusCode.Redirect,"non-owner management GET denied");using var post=await Post(client,"/owner/fields");Check(post.StatusCode==HttpStatusCode.Redirect,"non-owner creation denied");}
            using(var response=await Post(owner,"/owner/fields",csrf:false))Check(response.StatusCode==HttpStatusCode.BadRequest,"field mutation requires anti-forgery token");
            using(var response=await Post(owner,"/owner/fields",new(){{"Name"," "},{"HourlyPrice","-1"}}))Check(response.StatusCode==HttpStatusCode.BadRequest,"invalid required text and negative price rejected");
            using(var response=await Post(owner,"/owner/fields",new(){{"Format","11v11"}}))Check(response.StatusCode==HttpStatusCode.BadRequest,"unsupported field format rejected");
            using(var response=await Post(owner,"/owner/fields",file:"not a PNG"u8.ToArray()))Check(response.StatusCode==HttpStatusCode.BadRequest,"spoofed image rejected");
            using(var response=await Post(owner,"/owner/fields",file:new byte[5242881]))Check(response.StatusCode==HttpStatusCode.BadRequest,"oversized image rejected");
            using(var response=await Post(owner,"/owner/fields",new(){{"AmenityIds","2147483647"}}))Check(response.StatusCode==HttpStatusCode.BadRequest,"unknown amenity ID rejected");
            var amenity=await db.Amenities.OrderBy(x=>x.Id).FirstAsync();var second=await db.Amenities.Where(x=>x.Id!=amenity.Id).FirstAsync();
            using var created=await Post(owner,"/owner/fields",new(){{"Name",marker},{"AmenityIds",amenity.Id.ToString()},{"OwnerId",b.Id.ToString()},{"Rating","5"},{"IsArchived","true"}},Png);
            Check(created.StatusCode==HttpStatusCode.OK,"approved owner creates a field with image and amenities");var field=(await Json(created)).GetProperty("field");var id=field.GetProperty("id").GetInt32();var version=field.GetProperty("version").GetString()!;var photo=field.GetProperty("photo").GetString()!;
            var row=await db.Fields.AsNoTracking().SingleAsync(f=>f.Id==id);Check(row.OwnerId==a.Id && row.Rating==0 && !row.IsArchived,"forged ownership/rating/status ignored; publication policy enforced");Check(row.Name==marker && row.HourlyPrice==25.50m,"field persisted in SQL");Check(await db.FieldAmenities.AnyAsync(f=>f.FieldId==id&&f.AmenityId==amenity.Id),"amenity relationship persisted");Check(await db.FieldImages.CountAsync(i=>i.FieldId==id)==1,"image relationship persisted");
            using(var image=await visitor.GetAsync(photo))Check(image.IsSuccessStatusCode && (await image.Content.ReadAsByteArrayAsync()).SequenceEqual(Png) && image.Content.Headers.ContentType!.MediaType=="image/png","public image bytes and safe MIME served");
            using(var response=await other.GetAsync("/owner/fields/"+id))Check(response.StatusCode==HttpStatusCode.NotFound,"owner B cannot load owner A management data");
            using(var response=await Post(other,"/owner/fields/"+id,new(){{"Version",version}}))Check(response.StatusCode==HttpStatusCode.NotFound,"owner B cannot edit owner A field");
            using(var response=await Post(other,$"/owner/fields/{id}/archive",new(){{"Version",version}}))Check(response.StatusCode==HttpStatusCode.NotFound,"owner B cannot archive owner A field");
            Check((await owner.GetStringAsync("/owner/fields")).Contains(marker),"My Fields displays persisted field");Check(!(await other.GetStringAsync("/owner/fields")).Contains(marker),"My Fields isolates owners");
            using(var response=await visitor.GetAsync("/api/fields?Search="+marker)){var page=await Json(response);Check(page.GetProperty("total").GetInt32()==1,"public catalogue finds eligible SQL field");Check(!page.ToString().Contains("ownerId")&&!page.ToString().Contains("version\":\""),"public DTO omits private owner and edit credentials");}
            using(var response=await visitor.GetAsync("/api/fields?Search="+marker+"&City=Irbid"))Check((await Json(response)).GetProperty("total").GetInt32()==0,"city filter works");
            using(var response=await visitor.GetAsync("/api/field-cities"))Check((await Json(response)).EnumerateArray().Any(city=>city.GetString()=="Amman"),"catalogue location choices read eligible SQL cities");
            using(var response=await visitor.GetAsync("/api/fields?Search="+marker+"&Min=30"))Check((await Json(response)).GetProperty("total").GetInt32()==0,"price filter works");
            using(var response=await visitor.GetAsync("/api/fields?Min=100&Max=1"))Check(response.StatusCode==HttpStatusCode.BadRequest,"invalid range rejected");
            using(var response=await visitor.GetAsync("/api/fields?Page=0"))Check(response.StatusCode==HttpStatusCode.BadRequest,"invalid paging rejected");
            using(var response=await visitor.GetAsync("/api/fields/"+id))Check((await Json(response)).GetProperty("name").GetString()==marker,"details resolve correct SQL ID");
            using var edited=await Post(owner,"/owner/fields/"+id,new(){{"Name",marker+" edited"},{"Version",version},{"HourlyPrice","30"},{"AmenityIds",second.Id.ToString()}});Check(edited.IsSuccessStatusCode,"owner edits existing field");field=(await Json(edited)).GetProperty("field");Check(field.GetProperty("photo").GetString()==photo,"unrelated edit preserves image");Check(field.GetProperty("amenities")[0].GetProperty("id").GetInt32()==second.Id,"amenity selection updated");
            using(var stale=await Post(owner,"/owner/fields/"+id,new(){{"Version",version}}))Check(stale.StatusCode==HttpStatusCode.Conflict,"stale editor cannot overwrite newer changes");version=field.GetProperty("version").GetString()!;
            using(var replacement=await Post(owner,"/owner/fields/"+id,new(){{"Name",marker},{"Version",version}},Png)){Check(replacement.IsSuccessStatusCode,"image replacement commits without sort-order conflict");field=(await Json(replacement)).GetProperty("field");Check(field.GetProperty("photo").GetString()!=photo,"replacement receives a generated new URL");}
            using(var old=await visitor.GetAsync(photo))Check(old.StatusCode==HttpStatusCode.NotFound,"obsolete image is not delivered");version=field.GetProperty("version").GetString()!;photo=field.GetProperty("photo").GetString()!;
            using(var archived=await Post(owner,$"/owner/fields/{id}/archive",new(){{"Version",version}}))Check(archived.IsSuccessStatusCode,"field archived safely");
            using(var response=await visitor.GetAsync("/api/fields/"+id))Check(response.StatusCode==HttpStatusCode.NotFound,"archived field is not public");using(var response=await visitor.GetAsync(photo))Check(response.StatusCode==HttpStatusCode.NotFound,"archived image not public");using(var response=await owner.GetAsync(photo))Check(response.IsSuccessStatusCode,"owner retains access to own archived image");
            Check(await db.Fields.AnyAsync(f=>f.Id==id) && await db.FieldImages.AnyAsync(i=>i.FieldId==id),"archive retains field and image relationships");
            field=await Json(await owner.GetAsync("/owner/fields/"+id));version=field.GetProperty("version").GetString()!;
            using(var restore=await Post(owner,$"/owner/fields/{id}/restore",new(){{"Version",version}}))Check(restore.IsSuccessStatusCode,"owner restores field");
            field=await Json(await owner.GetAsync("/owner/fields/"+id));version=field.GetProperty("version").GetString()!;
            using(var remove=await Post(owner,"/owner/fields/"+id,new(){{"Name",marker},{"Version",version},{"RemovePhoto","true"}}))Check(remove.IsSuccessStatusCode && (await Json(remove)).GetProperty("field").GetProperty("images").GetArrayLength()==0,"explicit image removal persists");
            for(var index=0;index<13;index++)db.Fields.Add(new Field{OwnerId=a.Id,Name=marker+" page "+index,City="Irbid",Area="Test",Description="Temporary paging fixture",HourlyPrice=index+1,Format="6v6"});await db.SaveChangesAsync();
            using(var response=await visitor.GetAsync("/api/fields?Search="+marker+"&Format=6v6&Sort=price-low&Page=1")){var data=await Json(response);Check(data.GetProperty("total").GetInt32()==13&&data.GetProperty("items").GetArrayLength()==12,"public query bounds each page to twelve results");Check(data.GetProperty("items")[0].GetProperty("price").GetDecimal()==1,"ascending price sort is applied in SQL");}
            using(var response=await visitor.GetAsync("/api/fields?Search="+marker+"&Format=6v6&Sort=price-high&Page=2"))Check((await Json(response)).GetProperty("items").GetArrayLength()==1,"second page returns remaining matching row");
            a.Status="Inactive";await users.UpdateAsync(a);await users.UpdateSecurityStampAsync(a);using(var revoked=await owner.GetAsync("/owner/fields"))Check(revoked.StatusCode==HttpStatusCode.Redirect,"inactive owner's existing field-management cookie revoked");
            using(var hidden=await visitor.GetAsync("/api/fields/"+id))Check(hidden.StatusCode==HttpStatusCode.NotFound,"inactive owner's fields are hidden publicly");
        }finally{await Cleanup(db,imageStore,marker);Check(!await db.Users.AnyAsync(u=>u.Email!.StartsWith(marker)),"only temporary fixtures cleaned up");}
        Console.WriteLine($"Completed {passed} field integration assertions.");
    }
    static async Task Cleanup(KicksDbContext db,FieldImageStore store,string marker)
    {
        db.ChangeTracker.Clear();var ids=await db.Users.Where(u=>u.Email!.StartsWith(marker+"-")).Select(u=>u.Id).ToListAsync();var fields=await db.Fields.Where(f=>ids.Contains(f.OwnerId)).Select(f=>f.Id).ToListAsync();var keys=await db.FieldImages.Where(i=>fields.Contains(i.FieldId)).Select(i=>i.StorageUrl).ToListAsync();await using var tx=await db.Database.BeginTransactionAsync();
        await db.FieldAmenities.Where(f=>fields.Contains(f.FieldId)).ExecuteDeleteAsync();await db.FieldImages.Where(f=>fields.Contains(f.FieldId)).ExecuteDeleteAsync();await db.Fields.Where(f=>fields.Contains(f.Id)).ExecuteDeleteAsync();await db.UserClaims.Where(u=>ids.Contains(u.UserId)).ExecuteDeleteAsync();await db.UserLogins.Where(u=>ids.Contains(u.UserId)).ExecuteDeleteAsync();await db.UserTokens.Where(u=>ids.Contains(u.UserId)).ExecuteDeleteAsync();await db.UserRoles.Where(u=>ids.Contains(u.UserId)).ExecuteDeleteAsync();await db.Users.Where(u=>ids.Contains(u.Id)).ExecuteDeleteAsync();await tx.CommitAsync();foreach(var key in keys)store.Delete(key);
    }
}

