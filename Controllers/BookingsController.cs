using System.Security.Claims;
using KicksMvc.Services.Bookings;
using KicksMvc.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace KicksMvc.Controllers;

[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class BookingsController(BookingService bookings) : Controller
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private bool Owner => User.IsInRole("owner");

    [AllowAnonymous, HttpGet("/api/fields/{id:int}/availability")]
    public async Task<IActionResult> Availability(int id, DateOnly? date, int durationMinutes = 60)
    {
        if (!ModelState.IsValid || date is null || id < 1) return BadRequest(new { message = "Choose a valid date and field." });
        return await Execute(async () => Json(await bookings.AvailabilityAsync(id, date.Value, durationMinutes)));
    }
    [Authorize(Roles = "player"), HttpGet("/bookings/quote")]
    public async Task<IActionResult> Quote([FromQuery] BookingSlotInput input)
    {
        if (!ModelState.IsValid) return InvalidInput();
        return await Execute(async () => Json(await bookings.QuoteAsync(UserId, input)));
    }
    [Authorize(Roles = "player"), HttpPost("/bookings")]
    public async Task<IActionResult> Create(BookingCreateInput input)
    {
        if (!ModelState.IsValid) return InvalidInput();
        return await Execute(async () =>
        {
            var result = await bookings.CreateAsync(UserId, input.QuoteToken);
            var message = result.Booking.Status == "Cancelled" ? "This reservation was cancelled. No new reservation was created."
                : result.Replay ? "Existing reservation returned; no duplicate was created."
                : "Reservation confirmed. No payment has been collected.";
            return StatusCode(result.Replay ? 200 : 201, new { booking = result.Booking, replay = result.Replay, message });
        });
    }
    [Authorize(Roles = "player,owner"), HttpGet("/bookings")]
    public async Task<IActionResult> Index([FromQuery] BookingListInput input)
    {
        if (!ModelState.IsValid) return InvalidInput();
        ViewData["BookingOwner"] = Owner; ViewData["BookingSearch"] = input.Search;
        return PartialView("_Bookings", await bookings.ListAsync(UserId, Owner, input));
    }
    [Authorize(Roles = "player,owner"), HttpGet("/bookings/data")]
    public async Task<IActionResult> Data([FromQuery] BookingListInput input)
    {
        if (!ModelState.IsValid) return InvalidInput(); return Json(await bookings.ListAsync(UserId, Owner, input));
    }
    [Authorize(Roles = "player,owner"), HttpGet("/bookings/{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var booking = await bookings.GetAsync(id, UserId, Owner); return booking is null ? NotFound() : Json(booking);
    }
    [Authorize(Roles = "player,owner"), HttpPost("/bookings/{id:int}/cancel")]
    public Task<IActionResult> Cancel(int id, BookingStatusInput input) => ChangeStatus(id, input, false);
    [Authorize(Roles = "owner"), HttpPost("/bookings/{id:int}/complete")]
    public Task<IActionResult> Complete(int id, BookingStatusInput input) => ChangeStatus(id, input, true);
    private async Task<IActionResult> ChangeStatus(int id, BookingStatusInput input, bool complete)
    {
        if (!ModelState.IsValid) return InvalidInput();
        return await Execute(async () => await bookings.ChangeStatusAsync(id, UserId, Owner, input.Version, complete) ? Json(new { message = complete ? "Reservation completed." : "Reservation cancelled. History retained; no payment records changed." }) : NotFound());
    }
    private IActionResult InvalidInput() => BadRequest(new { message = "Check the date, 1- or 2-hour duration, page and submitted fields.", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
    private async Task<IActionResult> Execute(Func<Task<IActionResult>> action)
    {
        try { return await action(); }
        catch (BookingRuleException error) { return StatusCode(error.Conflict ? 409 : 400, new { message = error.Message }); }
        catch (DbUpdateException error) when (error.InnerException is SqlException { Number: 2601 or 2627 }) { return Conflict(new { message = "This slot was reserved by another request. Refresh availability." }); }
        catch (SqlException error) when (error.Number is 1205 or 1222) { return Conflict(new { message = "Another booking update is in progress. Refresh and retry." }); }
        catch (DbUpdateException) { return Conflict(new { message = "The reservation could not be saved. Refresh and retry." }); }
    }
}
