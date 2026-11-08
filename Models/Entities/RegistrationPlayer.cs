namespace KicksMvc.Models.Entities;

public class RegistrationPlayer
{
    public int RegistrationId { get; set; }
    public int UserId { get; set; }
    public string NameSnapshot { get; set; } = string.Empty;
    public string Position { get; set; } = "Midfielder";

    public TournamentRegistration Registration { get; set; } = null!;
}
