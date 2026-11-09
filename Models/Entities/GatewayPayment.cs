namespace KicksMvc.Models.Entities;

// Stripe sandbox ledger. Legacy demo payments and their distribution rules remain historical.
public sealed class GatewayPayment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int PayerId { get; set; }
    public int? BookingId { get; set; }
    public int? RegistrationId { get; set; }
    public decimal AmountJod { get; set; }
    public decimal UsdPerJod { get; set; }
    public long AmountUsdCents { get; set; }
    public string Currency { get; set; } = "usd";
    public string Status { get; set; } = "Pending";
    public DateTimeOffset HoldExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
    public bool FinancialReview { get; set; }
    public Booking? Booking { get; set; }
    public TournamentRegistration? Registration { get; set; }
    public ICollection<StripeCheckoutAttempt> Attempts { get; set; } = new List<StripeCheckoutAttempt>();
}

public sealed class StripeCheckoutAttempt
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PaymentId { get; set; }
    public string? SessionId { get; set; }
    public string? PaymentIntentId { get; set; }
    public string Status { get; set; } = "Creating";
    public DateTimeOffset CreatedAt { get; set; }
    public GatewayPayment Payment { get; set; } = null!;
}

public sealed class StripeWebhookEvent
{
    public string EventId { get; set; } = "";
    public string EventType { get; set; } = "";
    public DateTimeOffset ProcessedAt { get; set; }
    public string Outcome { get; set; } = "";
}
