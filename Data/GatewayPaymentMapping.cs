using KicksMvc.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace KicksMvc.Data;

internal static class GatewayPaymentMapping
{
    public static void Configure(ModelBuilder model)
    {
        model.Entity<GatewayPayment>(e =>
        {
            e.ToTable("gateway_payments", "dbo", t =>
            {
                t.HasCheckConstraint("CK_gateway_target", "CASE WHEN [BookingId] IS NULL THEN 0 ELSE 1 END + CASE WHEN [RegistrationId] IS NULL THEN 0 ELSE 1 END = 1");
                t.HasCheckConstraint("CK_gateway_amount", "[AmountJod]>0 AND [UsdPerJod]>0 AND [AmountUsdCents] BETWEEN 50 AND 99999999 AND [Currency]='usd'");
                t.HasCheckConstraint("CK_gateway_status", "[Status] IN ('Pending','Paid','Expired','Cancelled')");
            });
            e.HasKey(p => p.Id);
            e.Property(p => p.AmountJod).HasPrecision(12,2);
            e.Property(p => p.UsdPerJod).HasPrecision(12,6);
            e.Property(p => p.Currency).HasMaxLength(3).IsUnicode(false);
            e.Property(p => p.Status).HasMaxLength(16).IsUnicode(false);
            e.HasOne<User>().WithMany().HasForeignKey(p => p.PayerId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(p => p.Booking).WithMany().HasForeignKey(p => p.BookingId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(p => p.Registration).WithMany().HasForeignKey(p => p.RegistrationId).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(p => p.BookingId).IsUnique().HasFilter("[BookingId] IS NOT NULL");
            e.HasIndex(p => p.RegistrationId).IsUnique().HasFilter("[RegistrationId] IS NOT NULL AND [Status] = 'Pending'");
            e.HasIndex(p => new {p.Status,p.HoldExpiresAt});
        });
        model.Entity<StripeCheckoutAttempt>(e =>
        {
            e.ToTable("stripe_checkout_attempts", "dbo", t => t.HasCheckConstraint("CK_checkout_status", "[Status] IN ('Creating','Open','Paid','Expired')"));
            e.HasKey(a => a.Id);
            e.Property(a => a.SessionId).HasMaxLength(255).IsUnicode(false);
            e.Property(a => a.PaymentIntentId).HasMaxLength(255).IsUnicode(false);
            e.Property(a => a.Status).HasMaxLength(16).IsUnicode(false);
            e.HasOne(a => a.Payment).WithMany(p => p.Attempts).HasForeignKey(a => a.PaymentId).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(a => a.SessionId).IsUnique().HasFilter("[SessionId] IS NOT NULL");
            e.HasIndex(a => a.PaymentIntentId).IsUnique().HasFilter("[PaymentIntentId] IS NOT NULL");
        });
        model.Entity<StripeWebhookEvent>(e =>
        {
            e.ToTable("stripe_webhook_events", "dbo");
            e.HasKey(w => w.EventId);
            e.Property(w => w.EventId).HasMaxLength(255).IsUnicode(false);
            e.Property(w => w.EventType).HasMaxLength(100).IsUnicode(false);
            e.Property(w => w.Outcome).HasMaxLength(40).IsUnicode(false);
        });
    }
}
