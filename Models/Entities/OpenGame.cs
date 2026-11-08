namespace KicksMvc.Models.Entities;

public class OpenGame
{
    public int Id { get; set; }
    public int OrganizerId { get; set; }
    public int FieldId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTimeOffset StartsAt { get; set; }
    public int DurationMinutes { get; set; }
    public DateTimeOffset EndsAt { get; set; }
    public int Capacity { get; set; }
    public decimal PlayerFee { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Status { get; set; } = "Scheduled";
    public DateTimeOffset CreatedAt { get; set; }

    public Field Field { get; set; } = null!;
    public ICollection<GameParticipant> Participants { get; set; } = new List<GameParticipant>();
}
