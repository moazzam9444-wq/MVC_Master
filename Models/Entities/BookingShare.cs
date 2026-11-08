namespace KicksMvc.Models.Entities;

public class BookingShare
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public int TeamId { get; set; }
    public int PayerId { get; set; }
    public decimal Amount { get; set; }
    public decimal Commission { get; set; }
    public string PaymentState { get; set; } = "Unpaid";
    public DateTimeOffset? PaidAt { get; set; }

    public Booking Booking { get; set; } = null!;
    public User Payer { get; set; } = null!;
    public BookingTeam BookingTeam { get; set; } = null!;
    public Payment? Payment { get; set; }
}
