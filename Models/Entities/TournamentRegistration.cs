namespace KicksMvc.Models.Entities;

public class TournamentRegistration
{
    public int Id { get; set; }
    public int TournamentId { get; set; }
    public int TeamId { get; set; }
    public int CaptainId { get; set; }
    public string TeamNameSnapshot { get; set; } = string.Empty;
    public string CaptainNameSnapshot { get; set; } = string.Empty;
    public string EmailSnapshot { get; set; } = string.Empty;
    public string Status { get; set; } = "Registered";
    public DateTimeOffset? WithdrawnAt { get; set; }
    public DateTimeOffset? PaymentDeadline { get; set; }
    public int? WithdrawnBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Tournament Tournament { get; set; } = null!;
    public Team Team { get; set; } = null!;
    public ICollection<RegistrationPlayer> Players { get; set; } = new List<RegistrationPlayer>();
    public TournamentDrawEntry? DrawEntry { get; set; }
    public ICollection<TournamentMatchSlot> MatchSlots { get; set; } = new List<TournamentMatchSlot>();
}
