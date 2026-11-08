using System.ComponentModel.DataAnnotations;

namespace KicksMvc.ViewModels;

public class LoginViewModel
{
    [Required, EmailAddress, StringLength(254)]
    public string Email { get; set; } = "";
    [Required, StringLength(128), DataType(DataType.Password)]
    public string Password { get; set; } = "";
    [StringLength(2048)]
    public string? ReturnTo { get; set; }
}

public sealed class RegisterViewModel : LoginViewModel
{
    [Required, StringLength(100, MinimumLength = 2), Display(Name = "Full name")]
    public string FullName { get; set; } = "";
    // A request for review, never an authorization role assignment.
    [Required, RegularExpression("^(player|owner)$", ErrorMessage = "Choose Player or Field owner.")]
    public string Role { get; set; } = "player";
    [Required, Compare(nameof(Password)), DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = "";
    public IFormFile? OwnerProof { get; set; }
}

public sealed class AccountReviewViewModel
{
    [Range(1, int.MaxValue)] public int Id { get; set; }
    [Required, StringLength(256)] public string ConcurrencyStamp { get; set; } = "";
    [Required, RegularExpression("^(approve|reject)$")] public string Decision { get; set; } = "";
    [Required, RegularExpression("^(player|owner)$")] public string AssignedRole { get; set; } = "player";
}

public sealed class AccountStatusViewModel
{
    [Range(1, int.MaxValue)] public int Id { get; set; }
    [Required, StringLength(256)] public string ConcurrencyStamp { get; set; } = "";
    [Required, RegularExpression("^(Active|Inactive)$")] public string Status { get; set; } = "";
}

public sealed record AccountRowViewModel(int Id, string Name, string Email, string Status,
    string? RequestedRole, string? Role, string ConcurrencyStamp, DateTimeOffset? ReviewedAt,
    int? ReviewedBy, IReadOnlyList<AccountDocumentViewModel> Documents);
public sealed record AccountDocumentViewModel(int Id, string FileName);
