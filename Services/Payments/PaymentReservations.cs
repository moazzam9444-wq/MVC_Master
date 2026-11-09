using KicksMvc.Data;
using KicksMvc.Models.Entities;
using KicksMvc.Services.Activities;
using Microsoft.EntityFrameworkCore;

namespace KicksMvc.Services.Payments;

// Called inside the existing field/tournament transaction. No network or payment fabrication.
public sealed class PaymentReservations(KicksDbContext db, SandboxSettings settings, TimeProvider clock)
{
    public DateTimeOffset Deadline(DateTimeOffset cutoff,decimal amount)
    {
        if(amount<=0)return clock.GetUtcNow();
        settings.Convert(amount);
        var deadline=DateTimeOffset.FromUnixTimeSeconds(clock.GetUtcNow().ToUnixTimeSeconds()+1800);
        if(cutoff<clock.GetUtcNow().AddMinutes(30))throw new ActivityException("Start payment at least 30 minutes before the booking starts or tournament registration closes.",409);
        return deadline;
    }
    public async Task Add(Booking booking)
    {
        if(booking.Price<=0)return;
        var converted=settings.Convert(booking.Price);
        db.GatewayPayments.Add(new(){PayerId=booking.UserId,BookingId=booking.Id,AmountJod=booking.Price,
            UsdPerJod=converted.Rate,AmountUsdCents=converted.Cents,HoldExpiresAt=booking.PaymentDeadline!.Value,CreatedAt=clock.GetUtcNow()});
        await db.SaveChangesAsync();
    }
    public async Task Add(TournamentRegistration entry,decimal fee,int payerId)
    {
        if(fee<=0)return;
        var converted=settings.Convert(fee);
        db.GatewayPayments.Add(new(){PayerId=payerId,RegistrationId=entry.Id,AmountJod=fee,
            UsdPerJod=converted.Rate,AmountUsdCents=converted.Cents,HoldExpiresAt=entry.PaymentDeadline!.Value,CreatedAt=clock.GetUtcNow()});
        await db.SaveChangesAsync();
    }
    public async Task Cancel(int? bookingId=null,int? registrationId=null,int? tournamentId=null)
    {
        var rows=await db.GatewayPayments.Where(p=>(bookingId!=null&&p.BookingId==bookingId)||(registrationId!=null&&p.RegistrationId==registrationId)||(tournamentId!=null&&p.Registration!=null&&p.Registration.TournamentId==tournamentId)).ToListAsync();
        foreach(var p in rows)
        {
            if(p.Status=="Paid")p.FinancialReview=true;
            else if(p.Status=="Pending")
            {
                p.Status="Cancelled";
                if(p.RegistrationId is int rid){var r=await db.TournamentRegistrations.SingleAsync(r=>r.Id==rid);if(r.Status=="Pending"&&r.PaymentDeadline==p.HoldExpiresAt){r.Status="Withdrawn";r.WithdrawnAt=clock.GetUtcNow();}}
            }
        }
        await db.SaveChangesAsync();
    }
}
