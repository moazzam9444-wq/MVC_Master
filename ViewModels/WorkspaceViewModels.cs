using System.ComponentModel.DataAnnotations;
namespace KicksMvc.ViewModels;

public sealed record WorkspaceMetric(string Label, int Value, string Note, string Url);
public sealed record WorkspaceRow(string Title, string Detail, DateTimeOffset Date, string Status, string? Url = null, string? Context = null);
public sealed record WorkspaceOverview(string Role, IReadOnlyList<WorkspaceMetric> Metrics,
    IReadOnlyList<WorkspaceMetric> BookingStatuses, IReadOnlyList<WorkspaceRow> Bookings,
    IReadOnlyList<WorkspaceRow> Activity, string? TeamName, int TeamRequests);
public sealed record WorkspaceLedger(string Kind, int Page, int Total, IReadOnlyList<WorkspaceRow> Items);
public sealed class FeedbackInput
{
    [Required, StringLength(100, MinimumLength=2)] public string Name { get; set; } = "";
    [Required, EmailAddress, StringLength(254)] public string Email { get; set; } = "";
    [Range(1,5)] public int Rating { get; set; }
    [Required, StringLength(1500, MinimumLength=10)] public string Message { get; set; } = "";
}
public sealed class MessageInput
{
    [Range(1,int.MaxValue)] public int RecipientId { get; set; }
    [Required, StringLength(2000)] public string Body { get; set; } = "";
}
public sealed class SitePresentationInput
{
    [Required, StringLength(200)] public string Title { get; set; } = "";
    [Required, StringLength(1000)] public string Description { get; set; } = "";
    [StringLength(254)] public string? Contact { get; set; }
    [Required, StringLength(100)] public string City { get; set; } = "";
}
