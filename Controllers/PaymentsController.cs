using System.Security.Claims;
using KicksMvc.Services.Activities;
using KicksMvc.Services.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;

namespace KicksMvc.Controllers;

[ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public sealed class PaymentsController(StripePaymentService payments,SandboxSettings settings,TimeProvider clock):Controller
{
    int Actor=>int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    [Authorize(Roles="admin"),HttpGet("/payments/history")]
    public async Task<IActionResult> History(int page=1,bool review=false)=>page is <1 or >100000?BadRequest():PartialView("_PaymentHistory",await payments.History(page,review));
    [Authorize(Roles="admin"),HttpGet("/api/payments/history")]
    public async Task<IActionResult> HistoryData(int page=1,bool review=false)=>page is <1 or >100000?BadRequest():Json(await payments.History(page,review));
    [Authorize(Roles="player"),HttpGet("/payments/booking/{id:int}")]
    public async Task<IActionResult> Booking(int id)=>await payments.ForBooking(Actor,id) is Guid key?Redirect("/payments/"+key):NotFound();
    [Authorize(Roles="player"),HttpGet("/payments/tournament/{id:int}")]
    public async Task<IActionResult> Tournament(int id)=>await payments.ForTournament(Actor,id) is Guid key?Redirect("/payments/"+key):NotFound();
    [Authorize(Roles="player"),HttpGet("/payments/{id:guid}")]
    public async Task<IActionResult> Review(Guid id)
    {
        var payment=await payments.Find(Actor,id);if(payment is null)return NotFound();
        ViewData["HoldActive"]=payment.Status=="Pending"&&payment.HoldExpiresAt>clock.GetUtcNow();
        return View("~/Views/Payments/Review.cshtml",payment);
    }
    [Authorize(Roles="player"),HttpGet("/api/payments/{id:guid}")]
    public async Task<IActionResult> Status(Guid id)
    {
        var p=await payments.Find(Actor,id);return p is null?NotFound():Json(new{p.Id,p.Status,p.FinancialReview,p.HoldExpiresAt,HoldActive=p.Status=="Pending"&&p.HoldExpiresAt>clock.GetUtcNow()});
    }
    [Authorize(Roles="player"),HttpPost("/payments/{id:guid}/checkout")]
    public async Task<IActionResult> Checkout(Guid id)=>await Run(async()=>Redirect(await payments.Checkout(Actor,id)));
    [Authorize(Roles="player"),HttpPost("/payments/{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id)=>await Run(async()=>{await payments.CancelUnpaid(Actor,id);return Redirect("/payments/"+id);});
    [AllowAnonymous,IgnoreAntiforgeryToken,HttpPost("/payments/stripe/webhook"),RequestSizeLimit(1048576)]
    public async Task<IActionResult> Webhook()
    {
        return await Run(async()=>
        {
            var secret=settings.SigningSecret();Event stripeEvent;
            try
            {
                using var reader=new StreamReader(Request.Body);var json=await reader.ReadToEndAsync(HttpContext.RequestAborted);
                if(json.Length>1048576)return StatusCode(413);
                stripeEvent=EventUtility.ConstructEvent(json,Request.Headers["Stripe-Signature"].ToString(),secret,throwOnApiVersionMismatch:false);
                if(stripeEvent.Id is null||stripeEvent.Id.Length>200||stripeEvent.Type is null||stripeEvent.Type.Length>100)return BadRequest();
            }
            catch{return BadRequest(new{message="Invalid Stripe webhook."});}
            await payments.Receive(stripeEvent);return Ok(new{received=true});
        });
    }
    async Task<IActionResult> Run(Func<Task<IActionResult>> action)
    {
        try{return await action();}
        catch(ActivityException e){return PaymentError(e.Status,e.Message);}
        catch{return PaymentError(503,"Sandbox payment processing is temporarily unavailable. Retry safely.");}
    }
    IActionResult PaymentError(int status,string message)
    {
        if(Request.Headers.Accept.ToString().Contains("text/html",StringComparison.OrdinalIgnoreCase)&&Guid.TryParse(RouteData.Values["id"]?.ToString(),out var id))
        {
            Response.StatusCode=status;ViewData["PaymentReturnUrl"]="/payments/"+id;
            return View("~/Views/Payments/Unavailable.cshtml",message);
        }
        return StatusCode(status,new{message});
    }
}
