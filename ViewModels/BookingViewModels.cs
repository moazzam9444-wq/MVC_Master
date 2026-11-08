using System.ComponentModel.DataAnnotations;
namespace KicksMvc.ViewModels;

public sealed class BookingSlotInput
{
    [Range(1, int.MaxValue)] public int FieldId { get; set; }
    [Required] public DateOnly? Date { get; set; }
    [Range(13, 22)] public int Hour { get; set; }
    [AllowedValues(60, 120, ErrorMessage = "Choose a booking duration of exactly 1 or 2 hours.")] public int DurationMinutes { get; set; } = 60;
}
public sealed class BookingCreateInput
{
    [Required, StringLength(4096)] public string QuoteToken { get; set; } = "";
}
public sealed class BookingStatusInput
{
    [Required, StringLength(2000)] public string Version { get; set; } = "";
}
public sealed class BookingListInput
{
    [Range(1, 100000)] public int Page { get; set; } = 1;
    [StringLength(120)] public string? Search { get; set; }
    [RegularExpression("^(all|upcoming|history)$")] public string Period { get; set; } = "all";
}
public sealed record BookingSlot(int Hour, string Time, bool Available);
public sealed record AvailabilityResult(int FieldId, DateOnly Date, int DurationMinutes, string Timezone, IReadOnlyList<BookingSlot> Slots, decimal Price, string Currency);
public sealed record BookingQuote(int FieldId, string FieldName, DateTimeOffset StartsAt, DateTimeOffset EndsAt, int DurationMinutes, decimal Price, string Currency, string QuoteToken, DateTimeOffset ExpiresAt);
public sealed record BookingDisplay(int Id, int FieldId, string FieldName, string City, string Area, string PlayerName,
    DateTimeOffset StartsAt, DateTimeOffset EndsAt, int DurationMinutes, decimal Price, string Status, bool Ended,
    bool CanCancel, bool CanComplete, string Version, string? CancellationReason, DateTimeOffset? CancelledAt);
public sealed record BookingPage(IReadOnlyList<BookingDisplay> Items, int Total, int Page, int PageSize, string Period);
