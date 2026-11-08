using KicksMvc.Data;
using KicksMvc.Models.Entities;
using KicksMvc.Services.Fields;
using KicksMvc.ViewModels;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace KicksMvc.Services.Bookings;

public sealed class BookingRuleException(string message, bool conflict = false) : Exception(message)
{
    public bool Conflict { get; } = conflict;
}

public sealed class BookingService(KicksDbContext db, FieldService fields, IDataProtectionProvider protection, TimeProvider clock)
{
    private readonly IDataProtector quotes = protection.CreateProtector("Kicks.BookingQuote.v2");
    private readonly IDataProtector versions = protection.CreateProtector("Kicks.BookingVersion.v1");
    private DateTimeOffset Now => clock.GetUtcNow();
    private sealed record QuotePayload(int PlayerId, int FieldId, DateTimeOffset Start, int DurationMinutes, decimal Price, int HistoryWatermark, DateTimeOffset ExpiresAt);
    private static DateTimeOffset Start(DateOnly date, int hour) => new(date.ToDateTime(new TimeOnly(hour, 0)), TimeSpan.FromHours(3));
    private static void ValidateSlot(BookingSlotInput input)
    {
        if (input.Date is null || input.Hour is < 13 or > 22 || input.DurationMinutes is not (60 or 120) || input.FieldId < 1
            || input.Hour + input.DurationMinutes / 60 > 23)
            throw new BookingRuleException("Choose a 1- or 2-hour session starting on a full hour from 13:00 and ending by 23:00 (Amman).");
    }
    private static decimal TotalPrice(decimal hourlyPrice, int durationMinutes)
    {
        var hours = durationMinutes / 60m;
        if (durationMinutes is not (60 or 120) || hourlyPrice < 0 || hourlyPrice > 9999999999.99m / hours)
            throw new BookingRuleException("The reservation price is outside the supported range. Contact the field owner.");
        return decimal.Round(hourlyPrice * hours, 2, MidpointRounding.AwayFromZero);
    }
    private async Task<Field> Eligible(int fieldId)
    {
        var field = await fields.PublicFields.AsNoTracking().SingleOrDefaultAsync(f => f.Id == fieldId);
        return field ?? throw new BookingRuleException("This field is unavailable.", true);
    }
    // All booking creation/status changes and Phase 2 field edits/archive serialize on this row.
    private async Task<Field> LockField(int id) => await FieldOccupancy.Lock(db,id) ?? throw new BookingRuleException("This field is unavailable.", true);
    private Task<bool> Occupied(int fieldId, DateTimeOffset start, DateTimeOffset end) => FieldOccupancy.Occupied(db,fieldId,start,end);

    public async Task<AvailabilityResult> AvailabilityAsync(int fieldId, DateOnly date, int durationMinutes = 60)
    {
        ValidateSlot(new() { FieldId = fieldId, Date = date, Hour = 13, DurationMinutes = durationMinutes });
        var field = await Eligible(fieldId);
        if (date < DateOnly.FromDateTime(Now.ToOffset(TimeSpan.FromHours(3)).DateTime)) throw new BookingRuleException("Choose today or a future date.");
        var start = Start(date, 13); var end = Start(date, 23);
        var blocks = await FieldOccupancy.Blocks(db,fieldId,start,end);
        return new(fieldId, date, durationMinutes, "Amman (UTC+03:00)", Enumerable.Range(13, 11 - durationMinutes / 60).Select(hour => { var s = Start(date, hour); return new BookingSlot(hour, $"{hour:00}:00", s > Now && !blocks.Any(b => b.StartsAt < s.AddMinutes(durationMinutes) && b.EndsAt > s)); }).ToArray(), TotalPrice(field.HourlyPrice, durationMinutes), "JOD");
    }
    public async Task<BookingQuote> QuoteAsync(int playerId, BookingSlotInput input)
    {
        ValidateSlot(input); var start = Start(input.Date!.Value, input.Hour);
        if (start <= Now) throw new BookingRuleException("Choose a future date and time.");
        var field = await Eligible(input.FieldId);
        if (await Occupied(field.Id, start, start.AddMinutes(input.DurationMinutes))) throw new BookingRuleException("This slot is no longer available. Choose another time.", true);
        var watermark = await db.Bookings.Where(b => b.UserId == playerId && b.FieldId == field.Id && b.StartsAt == start && b.DurationMinutes == input.DurationMinutes).MaxAsync(b => (int?)b.Id) ?? 0;
        var payload = new QuotePayload(playerId, field.Id, start, input.DurationMinutes, TotalPrice(field.HourlyPrice, input.DurationMinutes), watermark, Now.AddMinutes(15));
        return new(field.Id, field.Name, start, start.AddMinutes(input.DurationMinutes), input.DurationMinutes, payload.Price, "JOD", quotes.Protect(JsonSerializer.Serialize(payload)), payload.ExpiresAt);
    }
    public async Task<(BookingDisplay Booking, bool Replay)> CreateAsync(int playerId, string quoteToken)
    {
        QuotePayload payload;
        try { payload = JsonSerializer.Deserialize<QuotePayload>(quotes.Unprotect(quoteToken)) ?? throw new CryptographicException(); }
        catch (Exception error) when (error is CryptographicException or JsonException or ArgumentException) { throw new BookingRuleException("The reservation quote is invalid. Select the slot again."); }
        if (payload.PlayerId != playerId) throw new BookingRuleException("This quote belongs to another session.");
        ValidateSlot(new() { FieldId = payload.FieldId, Date = DateOnly.FromDateTime(payload.Start.DateTime), Hour = payload.Start.Hour, DurationMinutes = payload.DurationMinutes });
        if (payload.Start.Offset != TimeSpan.FromHours(3) || payload.Start.TimeOfDay.Ticks % TimeSpan.TicksPerHour != 0)
            throw new BookingRuleException("The reservation quote has an invalid Amman start time. Select the slot again.");
        await using var transaction = await db.Database.BeginTransactionAsync(); await LockField(payload.FieldId);
        // A signed history watermark makes the first matching persisted result the durable replay result.
        // Cancelled results are returned too: an old confirmation cannot recreate a cancelled booking.
        var previous = await db.Bookings.AsNoTracking().Where(b => b.UserId == playerId && b.FieldId == payload.FieldId && b.StartsAt == payload.Start && b.DurationMinutes == payload.DurationMinutes && b.Id > payload.HistoryWatermark).OrderBy(b => b.Id).FirstOrDefaultAsync();
        if (previous is not null) { await transaction.CommitAsync(); return ((await GetAsync(previous.Id, playerId, false))!, true); }
        if (payload.ExpiresAt < Now || payload.Start <= Now) throw new BookingRuleException("This quote expired. Select the date and slot again.", true);
        var field = await Eligible(payload.FieldId);
        if (TotalPrice(field.HourlyPrice, payload.DurationMinutes) != payload.Price) throw new BookingRuleException("The field price changed. Review the new price before confirming.", true);
        if (await Occupied(field.Id, payload.Start, payload.Start.AddMinutes(payload.DurationMinutes))) throw new BookingRuleException("This slot was just reserved. Choose another time.", true);
        var booking = new Booking
        {
            FieldId = field.Id,
            UserId = playerId,
            OwnerId = field.OwnerId,
            StartsAt = payload.Start,
            DurationMinutes = payload.DurationMinutes,
            Price = payload.Price,
            CommissionRate = 0,
            Commission = 0,
            OwnerNet = payload.Price,
            Status = "Confirmed",
            IsSplit = false
        };
        db.Bookings.Add(booking); await db.SaveChangesAsync(); await transaction.CommitAsync();
        return ((await GetAsync(booking.Id, playerId, false))!, false);
    }
    private IQueryable<Booking> Scoped(int userId, bool owner) => owner ? db.Bookings.Where(b => b.OwnerId == userId && b.Field.OwnerId == userId) : db.Bookings.Where(b => b.UserId == userId);
    private string Stamp(Booking b) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { b.Id, b.FieldId, b.OwnerId, b.UserId, b.StartsAt, b.EndsAt, b.DurationMinutes, b.Price, b.Status, b.CancelledBy, b.CancelledAt, b.CancellationReason }))));
    private string Version(Booking b) => versions.Protect(Stamp(b));
    private static bool Simple(Booking b) => !b.IsSplit && b.ChallengeId is null && b.Teams.Count == 0 && b.Shares.Count == 0;
    private BookingDisplay Display(Booking b, bool owner) => new(b.Id, b.FieldId, b.Field.Name, b.Field.City, b.Field.Area, b.Player.FullName, b.StartsAt, b.EndsAt, b.DurationMinutes, b.Price, b.Status, b.EndsAt <= Now,
        Simple(b) && (owner ? (b.Status is "Pending" or "Confirmed") && b.StartsAt > Now : b.Status == "Confirmed" && b.StartsAt - Now >= TimeSpan.FromHours(3)),
        Simple(b) && owner && b.Status == "Confirmed" && b.EndsAt <= Now, Version(b), b.CancellationReason, b.CancelledAt);
    public async Task<BookingDisplay?> GetAsync(int id, int userId, bool owner)
    {
        var booking = await Scoped(userId, owner).AsNoTracking().Include(b => b.Field).Include(b => b.Player).Include(b => b.Teams).Include(b => b.Shares).AsSplitQuery().SingleOrDefaultAsync(b => b.Id == id);
        return booking is null ? null : Display(booking, owner);
    }
    public async Task<BookingPage> ListAsync(int userId, bool owner, BookingListInput input)
    {
        var query = Scoped(userId, owner); var now = Now;
        if (input.Period == "upcoming") query = query.Where(b => b.Status != "Cancelled" && b.EndsAt > now);
        if (input.Period == "history") query = query.Where(b => b.Status == "Cancelled" || b.EndsAt <= now);
        if (!string.IsNullOrWhiteSpace(input.Search)) { var term = input.Search.Trim(); query = query.Where(b => b.Field.Name.Contains(term) || b.Field.City.Contains(term) || b.Status.Contains(term)); }
        var total = await query.CountAsync(); const int size = 12; var page = Math.Min(input.Page, Math.Max(1, (int)Math.Ceiling(total / (double)size)));
        var bookings = await query.AsNoTracking().Include(b => b.Field).Include(b => b.Player).Include(b => b.Teams).Include(b => b.Shares).AsSplitQuery().OrderByDescending(b => b.StartsAt).ThenByDescending(b => b.Id).Skip((page - 1) * size).Take(size).ToListAsync();
        return new(bookings.Select(b => Display(b, owner)).ToArray(), total, page, size, input.Period);
    }
    public async Task<bool> ChangeStatusAsync(int id, int actorId, bool owner, string version, bool complete)
    {
        var fieldId = await Scoped(actorId, owner).Where(b => b.Id == id).Select(b => (int?)b.FieldId).SingleOrDefaultAsync();
        if (fieldId is null) return false;
        await using var transaction = await db.Database.BeginTransactionAsync(); await LockField(fieldId.Value);
        var booking = await Scoped(actorId, owner).Include(b => b.Teams).Include(b => b.Shares).AsSplitQuery().SingleOrDefaultAsync(b => b.Id == id);
        if (booking is null) return false;
        try { if (versions.Unprotect(version) != Stamp(booking)) throw new BookingRuleException("This reservation changed. Refresh its details before trying again.", true); }
        catch (CryptographicException) { throw new BookingRuleException("This reservation changed. Refresh its details before trying again.", true); }
        if (!Simple(booking)) throw new BookingRuleException("Team and split-payment reservations must use their dedicated workflow.", true);
        if (complete)
        {
            if (!owner || booking.Status != "Confirmed" || booking.EndsAt > Now) throw new BookingRuleException("Only the field owner can complete a confirmed session after it ends.", true);
            booking.Status = "Completed";
        }
        else
        {
            if (booking.Status is not ("Pending" or "Confirmed") || booking.StartsAt <= Now) throw new BookingRuleException("Only future active reservations can be cancelled.", true);
            if (!owner && (booking.Status != "Confirmed" || booking.StartsAt - Now < TimeSpan.FromHours(3))) throw new BookingRuleException("Players can cancel their own confirmed reservation only when at least 3 hours remain before its Amman start time. Please contact the field owner.", true);
            booking.Status = "Cancelled"; booking.CancellationReason = "Cancelled"; booking.CancelledBy = actorId; booking.CancelledAt = Now;
        }
        await db.SaveChangesAsync(); await transaction.CommitAsync(); return true;
    }
}
