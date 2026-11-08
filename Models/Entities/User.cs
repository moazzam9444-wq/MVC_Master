using Microsoft.AspNetCore.Identity;

namespace KicksMvc.Models.Entities;

public class User : IdentityUser<int>
{
    public string FullName { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public string? RequestedRole { get; set; }
    public int? ReviewedBy { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public string? City { get; set; }
    public string? Address { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<OwnershipDocument> OwnershipDocuments { get; set; } = new List<OwnershipDocument>();
    public ICollection<Field> OwnedFields { get; set; } = new List<Field>();
    public Team? CaptainedTeam { get; set; }
    public TeamMember? TeamMembership { get; set; }
}
