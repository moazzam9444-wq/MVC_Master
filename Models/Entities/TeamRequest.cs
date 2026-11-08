namespace KicksMvc.Models.Entities;

public class TeamRequest
{
    public int Id { get; set; }
    public int TeamId { get; set; }
    public int UserId { get; set; }
    public string RequestType { get; set; } = "join";
    public string Status { get; set; } = "Pending";
    public string Position { get; set; } = "Midfielder";
    public string? Message { get; set; }
    public int? ReviewedBy { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Team Team { get; set; } = null!;
}
