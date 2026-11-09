using System.Security.Claims;
using KicksMvc.Services.Activities;
using KicksMvc.Services.Tournaments;
using KicksMvc.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
namespace KicksMvc.Controllers;

[ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public sealed class TournamentApiController(TournamentService tournaments,KicksMvc.Services.Payments.StripePaymentService payments):Controller
{
    int Actor=>int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var id)?id:0;
    [AllowAnonymous,HttpGet("/api/tournaments")]public Task<IActionResult> List([FromQuery]TournamentListInput input)=>Run(async()=>{if(input.Mode!="browse"&&Actor==0)return Challenge();return Json(await tournaments.List(Actor,User.IsInRole("owner"),input));});
    [AllowAnonymous,HttpGet("/api/tournaments/{id:int}")]public Task<IActionResult> Details(int id)=>Run(async()=>Json(await tournaments.Details(Actor,id,User.IsInRole("owner"),User.IsInRole("player"))));
    [Authorize(Roles="owner,player,admin"),HttpGet("/api/tournaments/summary")]public Task<IActionResult> Summary()=>Run(async()=>Json(await tournaments.Summary(Actor,User.IsInRole("owner"),User.IsInRole("admin"))));
    [Authorize(Roles="owner"),HttpPost("/tournaments/quote")]public Task<IActionResult> Quote(TournamentInput input)=>Run(async()=>Json(await tournaments.QuoteCreation(Actor,input)));
    [Authorize(Roles="owner"),HttpPost("/tournaments")]public Task<IActionResult> Create(ActivityTokenInput input)=>Run(async()=>{var r=await tournaments.Create(Actor,input.Token);return StatusCode(r.Replay?200:201,new{r.Id,r.Replay});});
    [Authorize(Roles="owner"),HttpPost("/tournaments/{id:int}/edit")]public Task<IActionResult> Edit(int id,TournamentEditInput input)=>Run(async()=>{await tournaments.Edit(Actor,id,input);return Ok(new{message="Tournament updated."});});
    [Authorize(Roles="owner"),HttpPost("/tournaments/{id:int}/cancel")]public Task<IActionResult> Cancel(int id,ActivityVersionInput input)=>Run(async()=>{await tournaments.Cancel(Actor,id,input.Version);return Ok(new{message="Tournament cancelled. Future match time released; history retained. No refunds processed."});});
    [Authorize(Roles="player"),HttpPost("/tournaments/{id:int}/register")]public Task<IActionResult> Register(int id)=>Run(async()=>{var replay=await tournaments.Register(Actor,id);var key=await payments.ForTournament(Actor,id);var p=key is Guid pid?await payments.Find(Actor,pid):null;return StatusCode(replay?200:201,new{message=p?.Status=="Pending"?"Place held for 30 minutes; complete sandbox payment.":"Registration confirmed.",paymentUrl=p?.Status=="Pending"?"/payments/"+p.Id:null});});
    [Authorize(Roles="player"),HttpPost("/tournaments/{id:int}/withdraw")]public Task<IActionResult> Withdraw(int id)=>Run(async()=>{await tournaments.Withdraw(Actor,id);return Ok(new{message="Team withdrawn. Slot released; history retained."});});
    [Authorize(Roles="owner"),HttpPost("/tournaments/{id:int}/draw")]public Task<IActionResult> Draw(int id,ActivityVersionInput input)=>Run(async()=>{await tournaments.Generate(Actor,id,input.Version);return Ok(new{message="Draw locked. Draft fixtures do not reserve field time."});});
    [Authorize(Roles="owner"),HttpPost("/tournaments/{id:int}/matches/{match:int}/schedule")]public Task<IActionResult> Schedule(int id,int match,TournamentScheduleInput input)=>Run(async()=>{await tournaments.Schedule(Actor,id,match,input);return Ok(new{message="Match scheduled and field time reserved."});});
    [Authorize(Roles="owner"),HttpPost("/tournaments/{id:int}/matches/{match:int}/cancel")]public Task<IActionResult> CancelMatch(int id,int match,ActivityVersionInput input)=>Run(async()=>{await tournaments.CancelMatch(Actor,id,match,input.Version);return Ok(new{message="Unplayed match cancelled; field time released."});});
    [Authorize(Roles="owner"),HttpPost("/tournaments/{id:int}/matches/{match:int}/result")]public Task<IActionResult> Result(int id,int match,TournamentResultInput input)=>Run(async()=>{await tournaments.Result(Actor,id,match,input);return Ok(new{message="Result finalized. Winner advanced in SQL."});});
    async Task<IActionResult> Run(Func<Task<IActionResult>> action)
    {
        if(!ModelState.IsValid)return BadRequest(new{message="Check the required fields, 60/120-minute duration, capacity, fee precision and scores."});
        try{return await action();}catch(ActivityException e){return StatusCode(e.Status,new{message=e.Message});}catch(DbUpdateException){return Conflict(new{message="Another update changed this entry, fixture or schedule. Refresh and retry."});}catch(SqlException e)when(e.Number is 1205 or 1222){return Conflict(new{message="A concurrent update is in progress. Refresh and retry."});}
    }
}
