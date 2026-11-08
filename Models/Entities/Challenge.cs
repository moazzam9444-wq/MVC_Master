namespace KicksMvc.Models.Entities;

public class Challenge
{
    public int Id { get; set; }
    public int HostTeamId { get; set; }
    public int? OpponentTeamId { get; set; }
    public int PostedBy { get; set; }
    public int? FieldId { get; set; }
    public string City { get; set; } = string.Empty;
    public string Format { get; set; } = "5v5";
    public string Level { get; set; } = string.Empty;
    public DateTimeOffset StartsAt { get; set; }
    public string PaymentMode { get; set; } = "Split checkout";
    public string CostAgreement { get; set; } = "Half each";
    public decimal? QuotedPrice { get; set; }
    public string? Note { get; set; }
    public string Status { get; set; } = "Open";
    public DateTimeOffset CreatedAt { get; set; }

    public Team HostTeam { get; set; } = null!;
    public Team? OpponentTeam { get; set; }
    public Field? Field { get; set; }
    public ICollection<ChallengeRequest> Requests { get; set; } = new List<ChallengeRequest>();
    public Booking? Booking { get; set; }
}
