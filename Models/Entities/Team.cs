namespace KicksMvc.Models.Entities;

public class Team
{
    public int Id { get; set; }
    public int CaptainId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public string Level { get; set; } = "Intermediate";
    public string Format { get; set; } = "5v5";
    public int Capacity { get; set; } = 12;
    public string Bio { get; set; } = string.Empty;
    public string UsualMatchday { get; set; } = string.Empty;
    public string Initials { get; set; } = string.Empty;
    public string? BadgeUrl { get; set; }
    public string Color { get; set; } = "forest";
    public bool IsRecruiting { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }

    public User Captain { get; set; } = null!;
    public ICollection<TeamMember> Members { get; set; } = new List<TeamMember>();
    public ICollection<TeamRecruitingPosition> RecruitingPositions { get; set; } = new List<TeamRecruitingPosition>();
    public ICollection<TeamRequest> Requests { get; set; } = new List<TeamRequest>();
    public ICollection<TournamentRegistration> TournamentRegistrations { get; set; } = new List<TournamentRegistration>();
}
