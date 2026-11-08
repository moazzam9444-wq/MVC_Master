namespace KicksMvc.Models.Entities;

public class TeamMember
{
    public int UserId { get; set; }
    public int TeamId { get; set; }
    public string Position { get; set; } = "Midfielder";
    public DateTimeOffset JoinedAt { get; set; }

    public User User { get; set; } = null!;
    public Team Team { get; set; } = null!;
}
