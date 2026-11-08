namespace KicksMvc.Models.Entities;

public class GameParticipant
{
    public int Id { get; set; }
    public int GameId { get; set; }
    public int UserId { get; set; }
    public string Position { get; set; } = "Midfielder";
    public string Status { get; set; } = "Joined";
    public DateTimeOffset CreatedAt { get; set; }

    public OpenGame Game { get; set; } = null!;
    public User User { get; set; } = null!;
}
