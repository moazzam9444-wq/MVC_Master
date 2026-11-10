using System.Security.Claims;
using KicksMvc.Data;
using KicksMvc.Services.Activities;
using KicksMvc.Services.Fields;
using KicksMvc.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
namespace KicksMvc.Controllers;
[ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public sealed class ActivitiesApiController(OpenGameService games,ChallengeService challenges,FieldService fields):Controller
{
    int Actor=>int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var id)?id:0;
    bool Owner=>User.IsInRole("owner");
    [Authorize(Roles="player,owner"),HttpGet("/api/activities/summary")]public async Task<IActionResult> Summary()=>Json(await games.Summary(Actor,Owner));
    [AllowAnonymous,HttpGet("/api/games")]public Task<IActionResult> Games([FromQuery]ActivityListInput input)=>Run(async()=>{if(input.Mode!="browse"&&Actor==0)return Challenge();return Json(await games.List(Actor,Owner,input));});
    [AllowAnonymous,HttpGet("/api/games/{id:int}")]public Task<IActionResult> Game(int id)=>Run(async()=>Json(await games.Details(Actor,id)));
    [Authorize(Roles="owner"),HttpPost("/games/quote")]public Task<IActionResult> GameQuote(GameInput input)=>Run(async()=>Json(await games.QuoteGame(Actor,input)));
    [Authorize(Roles="owner"),HttpPost("/games")]public Task<IActionResult> CreateGame(ActivityTokenInput input)=>Run(async()=>{var r=await games.Create(Actor,input.Token);return StatusCode(r.Replay?200:201,new{r.Id,r.Replay});});
    [Authorize(Roles="player"),HttpPost("/games/{id:int}/join")]public Task<IActionResult> Join(int id,GameJoinInput input)=>Run(async()=>StatusCode(await games.Join(Actor,id,input.Position)?200:201,new{message="Joined immediately. No approval or payment required."}));
    [Authorize(Roles="player"),HttpPost("/games/{id:int}/leave")]public Task<IActionResult> Leave(int id)=>Run(async()=>{await games.Leave(Actor,id);return Ok(new{message="You left the game. Your place is available."});});
    [Authorize(Roles="owner"),HttpPost("/games/{id:int}/cancel")]public Task<IActionResult> Cancel(int id,ActivityVersionInput input)=>Run(async()=>{await games.Manage(Actor,id,input.Version,null);return Ok(new{message="Game cancelled. History retained; field time released."});});
    [Authorize(Roles="owner"),HttpPost("/games/{id:int}/edit")]public Task<IActionResult> Edit(int id,GameEditInput input)=>Run(async()=>{await games.Manage(Actor,id,input.Version,input);return Ok(new{message="Game updated."});});
    [Authorize(Roles="player"),HttpGet("/api/challenges")]public Task<IActionResult> Challenges([FromQuery]ActivityListInput input)=>Run(async()=>Json(await challenges.List(Actor,input)));
    [Authorize(Roles="player"),HttpGet("/api/challenges/{id:int}")]public Task<IActionResult> ChallengeDetails(int id)=>Run(async()=>Json(await challenges.Detail(Actor,id)));
    [Authorize(Roles="player"),HttpPost("/challenges")]public Task<IActionResult> CreateChallenge(ChallengeInput input)=>Run(async()=>StatusCode(201,new{Id=await challenges.Create(Actor,input)}));
    [Authorize(Roles="player"),HttpPost("/challenges/{id:int}/{decision:regex(^(accept|reject|withdraw)$)}")]public Task<IActionResult> Respond(int id,string decision,ActivityVersionInput input)=>Run(async()=>{await challenges.Respond(Actor,id,decision,input.Version);return Ok(new{message="Challenge updated. Check the reservation status; priced reservations require the challenging captain's Stripe sandbox payment."});});
    [Authorize(Roles="player,owner"),HttpGet("/api/activity-fields")]public async Task<IActionResult> Fields([FromQuery,System.ComponentModel.DataAnnotations.Range(1,100000)]int page=1){if(!ModelState.IsValid)return BadRequest();var q=fields.PublicFields;if(Owner)q=q.Where(f=>f.OwnerId==Actor);return Json(new{Items=await q.OrderBy(f=>f.Id).Skip((page-1)*20).Take(20).Select(f=>new{f.Id,f.Name,f.Format,f.HourlyPrice}).ToListAsync(),Total=await q.CountAsync(),Page=page});}
    async Task<IActionResult> Run(Func<Task<IActionResult>> action){if(!ModelState.IsValid)return BadRequest(new{message="Check the date, full-hour start, 60/120-minute duration and submitted fields.",errors=ModelState.Values.SelectMany(v=>v.Errors).Select(e=>e.ErrorMessage)});try{return await action();}catch(ActivityException e){return StatusCode(e.Status,new{message=e.Message});}catch(DbUpdateException){return Conflict(new{message="Another update changed this slot, participant or proposal. Refresh and retry."});}catch(SqlException e)when(e.Number is 1205 or 1222){return Conflict(new{message="Another scheduling update is in progress. Refresh and retry."});}}
}
