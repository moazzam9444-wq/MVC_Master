namespace KicksMvc.Models.Entities;

public class TournamentMatchSlot
{
    public int MatchId { get; set; }
    public int SlotNumber { get; set; }
    public int TournamentId { get; set; }
    public int? RegistrationId { get; set; }
    public int? SourceMatchId { get; set; }

    public TournamentMatche Match { get; set; } = null!;
    public TournamentMatche? SourceMatch { get; set; }
    public TournamentRegistration? Registration { get; set; }
}
