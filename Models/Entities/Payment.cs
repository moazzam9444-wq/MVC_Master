namespace KicksMvc.Models.Entities;

public class Payment
{
    public int Id { get; set; }
    public int PayerId { get; set; }
    public int PlatformAdminId { get; set; }
    public int? OwnerId { get; set; }
    public int? FieldId { get; set; }
    public int? BookingId { get; set; }
    public int? ShareId { get; set; }
    public int? GameParticipantId { get; set; }
    public int? RegistrationId { get; set; }
    public decimal Amount { get; set; }
    public decimal CommissionRate { get; set; }
    public decimal Commission { get; set; }
    public decimal OwnerNet { get; set; }
    public string Status { get; set; } = "Paid (demo)";
    public bool IsDemo { get; set; } = true;
    public DateTimeOffset? RefundedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public BookingShare? Share { get; set; }
    public Booking? Booking { get; set; }
    public GameParticipant? GameParticipant { get; set; }
    public TournamentRegistration? Registration { get; set; }
}
