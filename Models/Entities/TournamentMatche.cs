namespace KicksMvc.Models.Entities;

public class TournamentMatche
{
    public int Id { get; set; }
    public int TournamentId { get; set; }
    public int RoundNumber { get; set; }
    public int MatchNumber { get; set; }
    public int? WinnerRegistrationId { get; set; }
    public bool IsBye { get; set; } = false;
    public int? UpdatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public Tournament Tournament { get; set; } = null!;
    public ICollection<TournamentMatchSlot> InputSlots { get; set; } = new List<TournamentMatchSlot>();
    public ICollection<TournamentMatchSlot> OutputSlots { get; set; } = new List<TournamentMatchSlot>();
}
