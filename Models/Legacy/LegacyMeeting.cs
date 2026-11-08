namespace KicksMvc.Models.Legacy;

// Retained existing table only; no meeting feature or endpoint is reintroduced.
public class LegacyMeeting
{
    public int Id { get; set; }
    public int TournamentId { get; set; }
    public int OrganizerId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Agenda { get; set; }
    public DateTimeOffset StartsAt { get; set; }
    public int DurationMinutes { get; set; }
    public string RoomId { get; set; } = string.Empty;
    public string Status { get; set; } = "Scheduled";
    public DateTimeOffset CreatedAt { get; set; }
}
