namespace KicksMvc.Models.Entities;

public class TournamentDrawEntry
{
    public int RegistrationId { get; set; }
    public int TournamentId { get; set; }
    public int SeedPosition { get; set; }

    public TournamentRegistration Registration { get; set; } = null!;
    public Tournament Tournament { get; set; } = null!;
}
