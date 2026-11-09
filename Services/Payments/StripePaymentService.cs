using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using KicksMvc.Data;
using KicksMvc.Models.Entities;
using KicksMvc.Services.Activities;
using KicksMvc.Services.Bookings;
using KicksMvc.Services.Fields;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;
using KicksMvc.ViewModels;

namespace KicksMvc.Services.Payments;

public sealed class StripePaymentService(KicksDbContext db, SandboxSettings settings, FieldService fields, TimeProvider clock)
{
    DateTimeOffset Now=>clock.GetUtcNow();
    string Scope=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(db.Database.GetDbConnection().DataSource+"/"+db.Database.GetDbConnection().Database)));
    public async Task<GatewayPayment?> Find(int actor,Guid id)=>await db.GatewayPayments.AsNoTracking().SingleOrDefaultAsync(p=>p.Id==id&&p.PayerId==actor);
    public async Task<Guid?> ForBooking(int actor,int id)=>await db.GatewayPayments.Where(p=>p.BookingId==id&&p.PayerId==actor).Select(p=>(Guid?)p.Id).SingleOrDefaultAsync();
    public async Task<Guid?> ForTournament(int actor,int id)=>await db.GatewayPayments.Where(p=>p.Registration!=null&&p.Registration.TournamentId==id&&p.PayerId==actor).OrderByDescending(p=>p.CreatedAt).Select(p=>(Guid?)p.Id).FirstOrDefaultAsync();
    public async Task<PaymentHistoryPage> History(int page,bool review)
    {
        var all=db.GatewayPayments.AsNoTracking();var query=review?all.Where(p=>p.FinancialReview):all;
        var total=await query.CountAsync();page=Math.Min(page,Math.Max(1,(int)Math.Ceiling(total/20d)));
        var rows=await query.OrderByDescending(p=>p.CreatedAt).ThenBy(p=>p.Id).Skip((page-1)*20).Take(20)
            .Select(p=>new PaymentHistoryItem(p.Id,p.BookingId,p.RegistrationId,p.Booking!=null?p.Booking.Field.Name:p.Registration!.Tournament.Name,
                db.Users.Where(u=>u.Id==p.PayerId).Select(u=>u.FullName).First(),p.AmountJod,p.AmountUsdCents,p.UsdPerJod,p.Status,p.FinancialReview,p.CreatedAt)).ToListAsync();
        return new(rows,total,page,20,await all.Where(p=>p.Status=="Paid").SumAsync(p=>(long?)p.AmountUsdCents)??0,await all.CountAsync(p=>p.FinancialReview),review);
    }

    async Task<GatewayPayment> Gate(Guid id)
    {
        var target=await db.GatewayPayments.AsNoTracking().Where(p=>p.Id==id).Select(p=>new{p.BookingId,p.RegistrationId,Field=p.Booking!=null?p.Booking.FieldId:p.Registration!.Tournament.FieldId,Tournament=p.Registration!=null?(int?)p.Registration.TournamentId:null,Team=p.Registration!=null?(int?)p.Registration.TeamId:p.Booking!=null&&p.Booking.Challenge!=null?p.Booking.Challenge.HostTeamId:null,Other=p.Booking!=null&&p.Booking.Challenge!=null?p.Booking.Challenge.OpponentTeamId:null}).SingleOrDefaultAsync()??throw new ActivityException("Payment not found.",404);
        // Match registration, challenge and captain-transfer lock ordering: teams -> field -> tournament -> payment.
        foreach(var team in new[]{target.Team,target.Other}.Where(x=>x!=null).Select(x=>x!.Value).Distinct().Order())
            await db.Teams.FromSqlInterpolated($"SELECT * FROM dbo.teams WITH(UPDLOCK,HOLDLOCK) WHERE id={team}").SingleAsync();
        await FieldOccupancy.Lock(db,target.Field);
        if(target.Tournament is int tid)await db.Tournaments.FromSqlInterpolated($"SELECT * FROM dbo.tournaments WITH(UPDLOCK,HOLDLOCK) WHERE id={tid}").SingleAsync();
        var p=await db.GatewayPayments.FromSqlInterpolated($"SELECT * FROM dbo.gateway_payments WITH(UPDLOCK,HOLDLOCK) WHERE Id={id}").SingleAsync();
        return p;
    }
    async Task<bool> CanFulfil(GatewayPayment p)
    {
        if(p.Status!="Pending"||p.HoldExpiresAt<=Now||!await db.Users.AnyAsync(u=>u.Id==p.PayerId&&u.Status=="Active")||!await db.UserRoles.AnyAsync(ur=>ur.UserId==p.PayerId&&db.Roles.Any(r=>r.Id==ur.RoleId&&r.Name=="player")))return false;
        if(p.BookingId is int bid)return await db.Bookings.AnyAsync(b=>b.Id==bid&&b.UserId==p.PayerId&&b.Status=="Pending"&&b.PaymentDeadline>Now&&b.StartsAt>Now&&b.Price==p.AmountJod&&fields.PublicFields.Any(f=>f.Id==b.FieldId)&&
            (b.Challenge==null||b.Challenge.Status=="Awaiting payment"&&b.Challenge.HostTeam.CaptainId==p.PayerId&&b.Challenge.HostTeam.Format==b.Field.Format&&b.Challenge.OpponentTeam!=null&&b.Challenge.OpponentTeam.Format==b.Field.Format&&b.Challenge.OpponentTeam.Captain.Status=="Active"));
        return await db.TournamentRegistrations.AnyAsync(r=>r.Id==p.RegistrationId&&r.Team.CaptainId==p.PayerId&&r.Team.Format==r.Tournament.Field.Format&&r.Status=="Pending"&&r.PaymentDeadline>Now&&r.Tournament.EntryFee==p.AmountJod&&r.Tournament.Status=="Scheduled"&&r.Tournament.DrawStartedAt==null&&r.Tournament.RegistrationClosesAt>Now&&fields.PublicFields.Any(f=>f.Id==r.Tournament.FieldId)&&
            !r.Players.Any(member=>!db.Users.Any(u=>u.Id==member.UserId&&u.Status=="Active"&&db.UserRoles.Any(ur=>ur.UserId==u.Id&&db.Roles.Any(role=>role.Id==ur.RoleId&&role.Name=="player")))));
    }
    async Task Release(GatewayPayment p,bool expired)
    {
        if(p.BookingId is int bid)
        {
            var b=await db.Bookings.SingleAsync(b=>b.Id==bid);
            if(b.Status=="Pending"){b.Status="Cancelled";b.CancelledAt=Now;b.CancellationReason=expired?"Expired":"Cancelled";if(!expired)b.CancelledBy=p.PayerId;}
            if(b.Status=="Cancelled"&&b.ChallengeId is int cid){var c=await db.Challenges.SingleAsync(c=>c.Id==cid);if(c.Status=="Awaiting payment")c.Status=expired?"Expired":"Cancelled";}
        }
        if(p.RegistrationId is int rid)
        {
            var r=await db.TournamentRegistrations.SingleAsync(r=>r.Id==rid);
            // An older expired payment must never withdraw a newer registration hold.
            if(r.Status=="Pending"&&r.PaymentDeadline==p.HoldExpiresAt){r.Status="Withdrawn";r.WithdrawnAt=Now;if(!expired)r.WithdrawnBy=p.PayerId;}
        }
    }
    public async Task CancelUnpaid(int actor,Guid id)
    {
        await using var tx=await db.Database.BeginTransactionAsync();var p=await Gate(id);
        if(p.PayerId!=actor)throw new ActivityException("Payment not found.",404);
        if(p.Status=="Paid")throw new ActivityException("Use the booking or tournament cancellation rules. Paid cancellations require manual financial review.");
        if(p.Status=="Pending"){p.Status="Cancelled";await Release(p,false);await db.SaveChangesAsync();}await tx.CommitAsync();
        await CloseSessions(id);
    }
    public async Task<string> Checkout(int actor,Guid id)
    {
        settings.SigningSecret();var baseUrl=settings.BaseUrl();var client=settings.Client();
        Guid attemptId;
        DateTimeOffset attemptCreated;
        GatewayPayment snapshot;
        await using(var tx=await db.Database.BeginTransactionAsync())
        {
            var p=await Gate(id);if(p.PayerId!=actor)throw new ActivityException("Payment not found.",404);
            if(!await CanFulfil(p))throw new ActivityException("This checkout hold is no longer available. Refresh its status.");
            var attempt=await db.StripeCheckoutAttempts.Where(a=>a.PaymentId==id&&(a.Status=="Creating"||a.Status=="Open")).OrderByDescending(a=>a.CreatedAt).FirstOrDefaultAsync();
            if(attempt is null){attempt=new(){PaymentId=id,CreatedAt=Now};db.StripeCheckoutAttempts.Add(attempt);await db.SaveChangesAsync();}
            attemptId=attempt.Id;attemptCreated=attempt.CreatedAt;snapshot=p;await tx.CommitAsync();
        }
        // Never hold a SQL transaction across the network. Retries always use this durable attempt key.
        Session session;
        try
        {
            var balance=await client.V1.Balance.GetAsync();if(balance.Livemode)throw new ActivityException("Only Stripe sandbox is permitted.",503);
            session=await client.V1.Checkout.Sessions.CreateAsync(new SessionCreateOptions
            {
                Mode="payment",AllowedPaymentMethodTypes=["card"],AdaptivePricing=new(){Enabled=false},ClientReferenceId=id.ToString("N"),
                SuccessUrl=baseUrl+"/payments/"+id+"?return=success",CancelUrl=baseUrl+"/payments/"+id+"?return=cancel",
                // Stripe requires at least 30 minutes from API creation. SQL owns the exact 30-minute
                // hold; the worker explicitly expires Checkout then. One minute is transport margin.
                ExpiresAt=attemptCreated.AddMinutes(31).UtcDateTime,
                Metadata=new(){{"kicks_payment",id.ToString("N")},{"kicks_attempt",attemptId.ToString("N")},{"kicks_payer",actor.ToString(CultureInfo.InvariantCulture)},{"kicks_scope",Scope}},
                PaymentIntentData=new(){Metadata=new(){{"kicks_payment",id.ToString("N")},{"kicks_attempt",attemptId.ToString("N")},{"kicks_scope",Scope}}},
                LineItems=[new(){Quantity=1,PriceData=new(){Currency="usd",UnitAmount=snapshot.AmountUsdCents,
                    ProductData=new(){Name="KICKS sandbox reservation",Description=$"{snapshot.AmountJod:0.00} JOD at {snapshot.UsdPerJod:0.######} USD/JOD (test only)"}}}]
            },new RequestOptions{IdempotencyKey="kicks-checkout-"+attemptId.ToString("N")});
        }
        catch(ActivityException){throw;}
        catch{throw new ActivityException("Stripe sandbox could not open checkout. Retry safely from this payment page.",503);}
        if(session.Livemode||session.Currency!="usd"||session.AmountTotal!=snapshot.AmountUsdCents||string.IsNullOrWhiteSpace(session.Url)||!Uri.TryCreate(session.Url,UriKind.Absolute,out var checkoutUrl)||checkoutUrl.Scheme!="https"||checkoutUrl.Host!="checkout.stripe.com")throw new ActivityException("Stripe returned an unexpected sandbox session.",503);
        db.ChangeTracker.Clear(); // Reload state changed by webhooks/cancellation while the API call was in flight.
        await using(var tx=await db.Database.BeginTransactionAsync())
        {
            var p=await Gate(id);var attempt=await db.StripeCheckoutAttempts.SingleAsync(a=>a.Id==attemptId);
            if(attempt.SessionId is not null&&attempt.SessionId!=session.Id)throw new ActivityException("Checkout reference conflict.");
            attempt.SessionId=session.Id;attempt.PaymentIntentId=session.PaymentIntentId;if(attempt.Status=="Creating")attempt.Status="Open";
            await db.SaveChangesAsync();await tx.CommitAsync();
            if(!await CanFulfil(p)){await CloseSessions(id);throw new ActivityException("The hold ended while checkout was opening. No reservation was confirmed.");}
        }
        return session.Url;
    }
    static bool Matches(Session s,GatewayPayment p,StripeCheckoutAttempt a,string scope)=>!s.Livemode&&(a.SessionId is null||s.Id==a.SessionId)&&s.ClientReferenceId==p.Id.ToString("N")&&s.AmountTotal==p.AmountUsdCents&&s.Currency==p.Currency&&s.Metadata?.GetValueOrDefault("kicks_payment")==p.Id.ToString("N")&&s.Metadata?.GetValueOrDefault("kicks_attempt")==a.Id.ToString("N")&&s.Metadata?.GetValueOrDefault("kicks_payer")==p.PayerId.ToString(CultureInfo.InvariantCulture)&&s.Metadata?.GetValueOrDefault("kicks_scope")==scope;

    public async Task Receive(Event stripeEvent)
    {
        if(stripeEvent.Livemode)throw new ActivityException("Only test-mode events are accepted.",400);
        // The signed event must match the session bound to our own durable attempt, not just metadata.
        var supplied=stripeEvent.Data.Object as Session;
        var attempt=supplied is null?null:await db.StripeCheckoutAttempts.AsNoTracking().SingleOrDefaultAsync(a=>a.SessionId==supplied.Id);
        if(attempt is null&&supplied is not null&&Guid.TryParse(supplied.Metadata?.GetValueOrDefault("kicks_attempt"),out var recovering))
            attempt=await db.StripeCheckoutAttempts.AsNoTracking().SingleOrDefaultAsync(a=>a.Id==recovering&&a.SessionId==null);
        // Shared sandbox accounts can emit unrelated events. Never write them into this database.
        // A declined card leaves its open Checkout/hold retryable; only a paid Session can fulfil it.
        if(attempt is null)return;
        Session? latest=null;
        if(attempt is not null)
        {
            try{latest=await settings.Client().V1.Checkout.Sessions.GetAsync(supplied!.Id);}
            catch{throw new ActivityException("Sandbox confirmation is temporarily unavailable; delivery can be retried.",503);}
        }
        await using var tx=await db.Database.BeginTransactionAsync();
        var eventKey="kicks-stripe-event-"+stripeEvent.Id;
        await db.Database.ExecuteSqlInterpolatedAsync($"DECLARE @result int; EXEC @result=sys.sp_getapplock @Resource={eventKey},@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000; IF @result<0 THROW 51000,'Webhook busy',1;");
        if(await db.StripeWebhookEvents.AnyAsync(w=>w.EventId==stripeEvent.Id)){await tx.CommitAsync();return;}
        var outcome="Unrelated";
        if(attempt is not null&&supplied is not null&&latest is not null)
        {
            var p=await Gate(attempt.PaymentId);var a=await db.StripeCheckoutAttempts.SingleAsync(a=>a.Id==attempt.Id);
            if(a.SessionId is null&&Matches(supplied,p,a,Scope)&&Matches(latest,p,a,Scope))a.SessionId=latest.Id;
            if(!Matches(supplied,p,a,Scope)||!Matches(latest,p,a,Scope))outcome="RejectedReferencesOrAmount";
            else if(p.Status=="Paid")outcome="AlreadyPaid";
            else if(stripeEvent.Type is "checkout.session.completed" or "checkout.session.async_payment_succeeded" && latest.Status=="complete"&&latest.PaymentStatus=="paid"&&!string.IsNullOrWhiteSpace(latest.PaymentIntentId))
            {
                var valid=await CanFulfil(p);
                if(valid)
                {
                    if(p.BookingId is int bid){var b=await db.Bookings.SingleAsync(b=>b.Id==bid);b.Status="Confirmed";
                        if(b.ChallengeId is int cid){var c=await db.Challenges.SingleAsync(c=>c.Id==cid);c.Status="Booked";}}
                    if(p.RegistrationId is int rid){var r=await db.TournamentRegistrations.SingleAsync(r=>r.Id==rid);r.Status="Registered";}
                }
                else {await Release(p,true);p.FinancialReview=true;}
                p.Status="Paid";p.PaidAt=Now;a.Status="Paid";a.PaymentIntentId=latest.PaymentIntentId;outcome=valid?"Fulfilled":"PaidNeedsManualReview";
            }
            else if(latest.Status=="expired")
            {
                if(p.Status=="Pending"){await Release(p,true);p.Status="Expired";}a.Status="Expired";outcome="Expired";
            }
            else outcome="AwaitingPayment";
        }
        db.StripeWebhookEvents.Add(new(){EventId=stripeEvent.Id,EventType=stripeEvent.Type,ProcessedAt=Now,Outcome=outcome});
        await db.SaveChangesAsync();await tx.CommitAsync();
    }
    public async Task Sweep()
    {
        var ids=await db.GatewayPayments.AsNoTracking().Where(p=>p.Status=="Pending"&&p.HoldExpiresAt<=Now).OrderBy(p=>p.HoldExpiresAt).Take(50).Select(p=>p.Id).ToListAsync();
        foreach(var id in ids)
        {
            await using var tx=await db.Database.BeginTransactionAsync();var p=await Gate(id);
            if(p.Status=="Pending"&&p.HoldExpiresAt<=Now){await Release(p,true);p.Status="Expired";await db.SaveChangesAsync();}await tx.CommitAsync();
        }
        var close=await db.StripeCheckoutAttempts.AsNoTracking().Where(a=>(a.Status=="Open"||a.Status=="Creating")&&(a.Payment.Status=="Expired"||a.Payment.Status=="Cancelled")).OrderBy(a=>a.CreatedAt).Take(50).Select(a=>a.PaymentId).Distinct().ToListAsync();
        foreach(var id in close)await CloseSessions(id);
    }
    async Task CloseSessions(Guid paymentId)
    {
        var attempts=await db.StripeCheckoutAttempts.AsNoTracking().Where(a=>a.PaymentId==paymentId&&a.Status=="Open"&&a.SessionId!=null).ToListAsync();
        foreach(var a in attempts)
        {
            Session result;try{var client=settings.Client();result=await client.V1.Checkout.Sessions.GetAsync(a.SessionId!);if(result.Status=="open")result=await client.V1.Checkout.Sessions.ExpireAsync(a.SessionId!);}
            catch{continue;} // Retry next sweep. Never log SDK exceptions or treat a timeout as payment success.
            if(result.Livemode)continue;
            if(result.Status=="expired")await db.StripeCheckoutAttempts.Where(x=>x.Id==a.Id&&x.Status=="Open").ExecuteUpdateAsync(s=>s.SetProperty(x=>x.Status,"Expired"));
            // Completed sessions are confirmed only by a signed webhook; no redirect/poll fulfilment.
        }
    }
}
