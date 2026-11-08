using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using KicksMvc.Services.Teams;
using KicksMvc.ViewModels;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace KicksMvc.Controllers;

[Authorize(Roles="player"),ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public class TeamsController(TeamService teams,TeamLogoStore logos) : Controller
{
    int Actor=>int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    [HttpGet("/Pages/Teams/teams.html")]
    public IActionResult Index() => View();
    [HttpGet("/api/teams")] public Task<IActionResult> List([FromQuery]TeamListInput input)=>Run(async()=>Json(await teams.List(Actor,input)));
    [HttpGet("/api/teams/{id:int}")] public Task<IActionResult> Details(int id,[FromQuery,System.ComponentModel.DataAnnotations.Range(1,100000)]int page=1)=>Run(async()=>Json(await teams.Detail(Actor,id,page)));
    [HttpPost("/teams")] public Task<IActionResult> Create(TeamInput input)=>Run(async()=>StatusCode(201,new{id=await teams.Create(Actor,input)}));
    [HttpPost("/teams/{id:int}/edit")] public Task<IActionResult> Edit(int id,TeamInput input)=>Run(async()=>{await teams.Edit(Actor,id,input);return Ok(new{message="Team updated."});});
    [HttpPost("/teams/{id:int}/join")] public Task<IActionResult> Join(int id,TeamJoinInput input)=>Run(async()=>StatusCode(201,new{id=await teams.Join(Actor,id,input)}));
    [HttpPost("/teams/{id:int}/requests/{requestId:int}/{decision:regex(^(accept|reject)$)}")] public Task<IActionResult> Decide(int id,int requestId,string decision,TeamVersionInput input)=>Run(async()=>{await teams.Decide(Actor,id,requestId,decision=="accept",input.Version);return Ok(new{message="Request reviewed."});});
    [HttpPost("/teams/requests/{requestId:int}/cancel")] public Task<IActionResult> Cancel(int requestId)=>Run(async()=>{await teams.CancelRequest(Actor,requestId);return Ok(new{message="Request cancelled."});});
    [HttpPost("/teams/{id:int}/members/{userId:int}/remove")] public Task<IActionResult> Remove(int id,int userId,TeamVersionInput input)=>Run(async()=>{await teams.Remove(Actor,id,userId,input.Version,false);return Ok(new{message="Member removed; history preserved."});});
    [HttpPost("/teams/{id:int}/leave")] public Task<IActionResult> Leave(int id,TeamVersionInput input)=>Run(async()=>{await teams.Remove(Actor,id,Actor,input.Version,true);return Ok(new{message="You left the team."});});
    [HttpPost("/teams/{id:int}/transfer/{userId:int}")] public Task<IActionResult> Transfer(int id,int userId,TeamVersionInput input)=>Run(async()=>{await teams.Transfer(Actor,id,userId,input.Version);return Ok(new{message="Captaincy transferred."});});
    [HttpPost("/teams/{id:int}/logo"),RequestSizeLimit(1200000)] public Task<IActionResult> Logo(int id,TeamVersionInput input,IFormFile? logo)=>Run(async()=>{
        // Verify captain access before accepting private file storage work.
        var details=await teams.Detail(Actor,id);var json=System.Text.Json.JsonSerializer.SerializeToElement(details);if(!json.GetProperty("IsCaptain").GetBoolean())return StatusCode(403,new{message="Only this team's captain may upload its logo."});
        if(logo is null)return BadRequest(new{message="Choose an image."});string? key=null;
        try{key=await logos.Save(logo);var old=await teams.SetLogo(Actor,id,input.Version,"/media/teams/"+key);if(old is not null)logos.Delete(old.Split('/').Last());return Ok(new{message="Logo updated."});}catch{if(key is not null)logos.Delete(key);throw;}
    });
    [HttpGet("/media/teams/{key}")] public IActionResult Image(string key){try{var path=logos.PathFor(key);return System.IO.File.Exists(path)?PhysicalFile(path,Path.GetExtension(key) switch{".png"=>"image/png",".webp"=>"image/webp",_=>"image/jpeg"}):NotFound();}catch(InvalidDataException){return NotFound();}}
    async Task<IActionResult> Run(Func<Task<IActionResult>> action)
    {
        if(!ModelState.IsValid)return BadRequest(new{message="Check the submitted team details.",errors=ModelState.Values.SelectMany(v=>v.Errors).Select(e=>e.ErrorMessage)});
        try{return await action();}catch(TeamRuleException error){return StatusCode(error.Status,new{message=error.Message});}catch(InvalidDataException error){return BadRequest(new{message=error.Message});}catch(DbUpdateException){return Conflict(new{message="Team name or membership changed. Refresh and retry."});}catch(SqlException error)when(error.Number is 1205 or 1222){return Conflict(new{message="Another team update is in progress. Refresh and retry."});}catch(IOException){return StatusCode(503,new{message="Logo storage is unavailable. Please retry later."});}catch(UnauthorizedAccessException){return StatusCode(503,new{message="Logo storage is unavailable. Please retry later."});}
    }
}
