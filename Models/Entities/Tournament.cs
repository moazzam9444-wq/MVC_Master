namespace KicksMvc.Models.Entities;

public class Tournament
{
    public int Id { get; set; }
    public int CreatedBy { get; set; }
    public int FieldId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset StartsAt { get; set; }
    public int DurationMinutes { get; set; }
    public DateTimeOffset EndsAt { get; set; }
    public int Capacity { get; set; }
    public decimal EntryFee { get; set; }
    public string Prize { get; set; } = string.Empty;
    public string System { get; set; } = "Single elimination";
    public string Level { get; set; } = string.Empty;
    public DateTimeOffset? DrawStartedAt { get; set; }
    public string Status { get; set; } = "Scheduled";
    public int? CancelledBy { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Field Field { get; set; } = null!;
    public ICollection<TournamentRegistration> Registrations { get; set; } = new List<TournamentRegistration>();
    public ICollection<TournamentDrawEntry> DrawEntries { get; set; } = new List<TournamentDrawEntry>();
    public ICollection<TournamentMatche> Matches { get; set; } = new List<TournamentMatche>();
}
