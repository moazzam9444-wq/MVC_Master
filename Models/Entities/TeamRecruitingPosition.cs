namespace KicksMvc.Models.Entities;

public class TeamRecruitingPosition
{
    public int TeamId { get; set; }
    public string Position { get; set; } = string.Empty;

    public Team Team { get; set; } = null!;
}
