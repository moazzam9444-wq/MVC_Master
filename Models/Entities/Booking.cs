namespace KicksMvc.Models.Entities;

public class Booking
{
    public int Id { get; set; }
    public int FieldId { get; set; }
    public int UserId { get; set; }
    public int OwnerId { get; set; }
    public int? ChallengeId { get; set; }
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }
    public decimal Price { get; set; }
    public decimal CommissionRate { get; set; }
    public decimal Commission { get; set; }
    public decimal OwnerNet { get; set; }
    public bool IsSplit { get; set; } = false;
    public DateTimeOffset? PaymentDeadline { get; set; }
    public string Status { get; set; } = "Confirmed";
    public string? CancellationReason { get; set; }
    public int? CancelledBy { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Field Field { get; set; } = null!;
    public User Player { get; set; } = null!;
    public User Owner { get; set; } = null!;
    public Challenge? Challenge { get; set; }
    public ICollection<BookingTeam> Teams { get; set; } = new List<BookingTeam>();
    public ICollection<BookingShare> Shares { get; set; } = new List<BookingShare>();
}
