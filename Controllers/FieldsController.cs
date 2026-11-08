using Microsoft.AspNetCore.Mvc;
using KicksMvc.Services.Fields;
using KicksMvc.ViewModels;
using KicksMvc.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace KicksMvc.Controllers;

public class FieldsController(FieldService fields, FieldImageStore images, KicksDbContext db) : Controller
{
    [HttpGet("/Pages/Fields/fields.html")]
    public IActionResult Index() => View();

    [HttpGet("/Pages/Fields/field-details.html")]
    public IActionResult Details() => View();

    [HttpGet("/api/fields")]
    public async Task<IActionResult> Catalogue([FromQuery] FieldQuery query)
    {
        if (query.Min > query.Max) ModelState.AddModelError("Max", "Maximum price must be at least the minimum price.");
        if (!ModelState.IsValid) return BadRequest(new { message = "Check search, price ranges, format and page number." });
        return Json(await fields.ListAsync(query));
    }
    [HttpGet("/api/fields/{id:int}")]
    public async Task<IActionResult> DetailData(int id) { var field = await fields.GetAsync(id); return field is null ? NotFound() : Json(field); }
    [HttpGet("/api/field-cities")]
    public async Task<IActionResult> Cities() => Json(await fields.PublicFields.Select(f => f.City).Distinct().OrderBy(city => city).Take(100).ToListAsync());
    [HttpGet("/media/fields/{key}")]
    public async Task<IActionResult> Image(string key)
    {
        string path; try { path = images.PathFor(key); } catch (InvalidDataException) { return NotFound(); }
        var ownerId = User.IsInRole("owner") ? int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!) : -1;
        var allowed = await db.FieldImages.AnyAsync(i => i.StorageUrl == key && (fields.PublicFields.Any(f => f.Id == i.FieldId) || (ownerId > 0 && i.Field.OwnerId == ownerId)));
        if (!allowed || !System.IO.File.Exists(path)) return NotFound();
        Response.Headers.XContentTypeOptions = "nosniff"; Response.Headers.ContentSecurityPolicy = "default-src 'none'; sandbox";
        Response.Headers.CacheControl = "no-store";
        return PhysicalFile(path, Path.GetExtension(key) switch { ".png" => "image/png", ".webp" => "image/webp", _ => "image/jpeg" });
    }

}
