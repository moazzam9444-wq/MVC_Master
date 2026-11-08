using KicksMvc.Services.Fields;
using KicksMvc.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
namespace KicksMvc.Controllers;

[Authorize(Roles = "owner")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class OwnerFieldsController(FieldService fields) : Controller
{
    private int OwnerId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    [HttpGet("/owner/fields")]
    public async Task<IActionResult> Index(int page = 1) => PartialView("_OwnerFields", await fields.ListAsync(new FieldQuery { Page = Math.Clamp(page, 1, 100000) }, OwnerId));
    [HttpGet("/owner/fields-data")]
    public async Task<IActionResult> Summary() => Json(await fields.ListAsync(new FieldQuery(), OwnerId));
    [HttpGet("/owner/fields/{id:int}")]
    public async Task<IActionResult> Get(int id) { var field = await fields.GetAsync(id, OwnerId); return field is null ? NotFound() : Json(field); }
    [HttpGet("/owner/field-amenities")]
    public async Task<IActionResult> Amenities() => Json(await fields.AmenitiesAsync());
    [HttpPost("/owner/fields"), RequestSizeLimit(5500000), RequestFormLimits(MultipartBodyLengthLimit = 5400000)]
    public Task<IActionResult> Create(FieldInputViewModel input) => Save(null, input);
    [HttpPost("/owner/fields/{id:int}"), RequestSizeLimit(5500000), RequestFormLimits(MultipartBodyLengthLimit = 5400000)]
    public Task<IActionResult> Edit(int id, FieldInputViewModel input) => Save(id, input);
    private async Task<IActionResult> Save(int? id, FieldInputViewModel input)
    {
        if (string.IsNullOrWhiteSpace(input.Name) || input.Name.Trim().Length < 2) ModelState.AddModelError("Name", "Enter a field name of at least two characters.");
        if (string.IsNullOrWhiteSpace(input.Description) || input.Description.Trim().Length < 10) ModelState.AddModelError("Description", "Describe the field in at least ten characters.");
        if (decimal.Round(input.HourlyPrice, 2) != input.HourlyPrice) ModelState.AddModelError("HourlyPrice", "Use at most two decimal places.");
        if (!ModelState.IsValid) return BadRequest(new { errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
        try { var saved = await fields.SaveAsync(OwnerId, id, input); return saved is null ? NotFound() : Json(new { message = id == null ? "Field published." : "Field updated.", field = saved }); }
        catch (FieldConflictException) { return Conflict(new { message = "This field changed after you opened it. Close the editor, reload and try again." }); }
        catch (InvalidDataException error) { return BadRequest(new { message = error.Message }); }
        catch (DbUpdateException) { return Conflict(new { message = "The field could not be saved. Reload and try again." }); }
        catch (IOException) { return StatusCode(503, new { message = "Image storage is temporarily unavailable. Please retry." }); }
    }
    [HttpPost("/owner/fields/{id:int}/archive")]
    public Task<IActionResult> Archive(int id, FieldVersionInput input) => ArchiveState(id, input, true);
    [HttpPost("/owner/fields/{id:int}/restore")]
    public Task<IActionResult> Restore(int id, FieldVersionInput input) => ArchiveState(id, input, false);
    private async Task<IActionResult> ArchiveState(int id, FieldVersionInput input, bool archived)
    {
        if (!ModelState.IsValid) return BadRequest();
        try { return await fields.SetArchivedAsync(OwnerId, id, input.Version, archived) ? Json(new { message = archived ? "Field archived. History retained." : "Field restored." }) : NotFound(); }
        catch (FieldConflictException) { return Conflict(new { message = "This field changed. Reload and try again." }); }
        catch (DbUpdateException) { return Conflict(new { message = "The field could not be updated. Reload and try again." }); }
    }
}
