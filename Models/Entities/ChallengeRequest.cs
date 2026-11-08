namespace KicksMvc.Models.Entities;

public class ChallengeRequest
{
    public int Id { get; set; }
    public int ChallengeId { get; set; }
    public int TeamId { get; set; }
    public string Status { get; set; } = "Pending";
    public string? Note { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Challenge Challenge { get; set; } = null!;
    public Team Team { get; set; } = null!;
}
