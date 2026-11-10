using System.Security.Claims;
using KicksMvc.Data;
using KicksMvc.Models.Entities;
using KicksMvc.Services.Workspace;
using KicksMvc.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
namespace KicksMvc.Controllers;

public sealed class WorkspaceController(KicksDbContext db,WorkspaceService workspace):Controller
{
    private int Actor=>int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    [Authorize(Roles="player,owner,admin"),HttpGet("/dashboard/overview")]
    public async Task<IActionResult> Overview()=>PartialView("_WorkspaceOverview",await workspace.Overview(Actor,User.IsInRole("admin")?"admin":User.IsInRole("owner")?"owner":"player"));
    [Authorize(Roles="admin"),HttpGet("/dashboard/records/{kind:regex(^(bookings|feedback|fields)$)}")]
    public async Task<IActionResult> Records(string kind,int page=1,string? search=null){ViewData["Search"]=search;return PartialView("_WorkspaceRecords",await workspace.Ledger(kind,page,search));}

    [AllowAnonymous,EnableRateLimiting("feedback"),HttpPost("/feedback")]
    public async Task<IActionResult> Feedback(FeedbackInput input)
    {
        if(!ModelState.IsValid||string.IsNullOrWhiteSpace(input.Message)||input.Message.Trim().Length<10)return BadRequest(new{message="Enter your name, a valid email, a rating, and 10–1500 characters of feedback."});
        db.Feedback.Add(new Feedback{Name=input.Name.Trim(),Email=input.Email.Trim(),Rating=input.Rating,Message=input.Message.Trim()});await db.SaveChangesAsync();
        return Ok(new{message="Thank you. Your feedback was saved and is available to the KICKS administrator."});
    }
    [AllowAnonymous,HttpGet("/api/site-presentation")]
    public async Task<IActionResult> Presentation()=>Json(await db.Settings.AsNoTracking().Where(s=>s.Id==1).Select(s=>new{s.Title,s.Description,s.Contact,s.City}).SingleOrDefaultAsync());
    [Authorize(Roles="admin"),HttpGet("/dashboard/settings")]
    public async Task<IActionResult> Settings()
    {
        var s=await db.Settings.AsNoTracking().SingleOrDefaultAsync(s=>s.Id==1);
        return PartialView("_SitePresentation",new SitePresentationInput{Title=s?.Title??"Football brings us together.",Description=s?.Description??"Find your pitch, meet your team and make your next match happen.",City=s?.City??"Jordan",Contact=s?.Contact});
    }
    [Authorize(Roles="admin"),HttpPost("/dashboard/settings")]
    public async Task<IActionResult> SaveSettings(SitePresentationInput input)
    {
        if(!ModelState.IsValid)return BadRequest(new{message="Complete the title, description and city within their displayed limits."});
        var s=await db.Settings.SingleOrDefaultAsync(s=>s.Id==1);
        if(s is null){s=new Setting();db.Settings.Add(s);}
        s.Title=input.Title.Trim();s.Description=input.Description.Trim();s.Contact=input.Contact?.Trim();s.City=input.City.Trim();
        // Existing legacy financial fields are deliberately never assigned here.
        await db.SaveChangesAsync();return Ok(new{message="Site presentation saved."});
    }
    [Authorize(Roles="player,owner,admin"),HttpGet("/api/messages/contacts")]
    public async Task<IActionResult> Contacts(int page=1,string? search=null)
    {
        var q=workspace.Contacts(Actor);if(!string.IsNullOrWhiteSpace(search)){search=search.Trim();if(search.Length>100)search=search[..100];q=q.Where(u=>u.FullName.Contains(search));}
        var total=await q.CountAsync();page=Math.Clamp(page,1,Math.Max(1,(total+19)/20));
        return Json(new{total,page,items=await q.OrderBy(u=>u.FullName).ThenBy(u=>u.Id).Skip((page-1)*20).Take(20).Select(u=>new{u.Id,name=u.FullName}).ToListAsync()});
    }
    [Authorize(Roles="player,owner,admin"),HttpGet("/api/messages/{other:int}")]
    public async Task<IActionResult> Thread(int other,int before=int.MaxValue)
    {
        if(!await workspace.Contacts(Actor).AnyAsync(u=>u.Id==other))return NotFound(new{message="This conversation is unavailable."});
        var rows=await db.Messages.AsNoTracking().Where(m=>m.Id<before&&((m.SenderId==Actor&&m.RecipientId==other)||(m.SenderId==other&&m.RecipientId==Actor))).OrderByDescending(m=>m.Id).Take(50).Select(m=>new{m.Id,m.SenderId,m.Body,m.CreatedAt,m.ReadAt}).ToListAsync();
        rows.Reverse();return Json(new{items=rows,hasOlder=rows.Count==50});
    }
    [Authorize(Roles="player,owner,admin"),EnableRateLimiting("messages"),HttpPost("/messages")]
    public async Task<IActionResult> Send(MessageInput input)
    {
        if(!ModelState.IsValid||string.IsNullOrWhiteSpace(input.Body))return BadRequest(new{message="Write a message of 1–2000 characters."});
        if(!await workspace.Contacts(Actor).AnyAsync(u=>u.Id==input.RecipientId))return NotFound(new{message="This conversation is unavailable."});
        db.Messages.Add(new Message{SenderId=Actor,RecipientId=input.RecipientId,Body=input.Body.Trim()});await db.SaveChangesAsync();return Ok(new{message="Message sent."});
    }
    [Authorize(Roles="player,owner,admin"),HttpPost("/messages/{other:int}/read")]
    public async Task<IActionResult> Read(int other)
    {
        if(!await workspace.Contacts(Actor).AnyAsync(u=>u.Id==other))return NotFound();
        await db.Messages.Where(m=>m.RecipientId==Actor&&m.SenderId==other&&m.ReadAt==null).ExecuteUpdateAsync(s=>s.SetProperty(m=>m.ReadAt,DateTimeOffset.UtcNow));return Ok();
    }
}
