namespace KicksMvc.Models.Entities;

public class BookingTeam
{
    public int BookingId { get; set; }
    public int TeamId { get; set; }
    public int CaptainId { get; set; }
    public string TeamNameSnapshot { get; set; } = string.Empty;

    public Booking Booking { get; set; } = null!;
    public Team Team { get; set; } = null!;
    public User Captain { get; set; } = null!;
    public BookingShare? Share { get; set; }
}
