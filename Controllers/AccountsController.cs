using KicksMvc.Data;
using KicksMvc.Models.Entities;
using KicksMvc.Models.Identity;
using KicksMvc.Services.Authentication;
using KicksMvc.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace KicksMvc.Controllers;

[Authorize(Roles = KicksRoles.Admin)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class AccountsController(KicksDbContext db, UserManager<User> users, OwnershipDocumentStore documents) : Controller
{
    [HttpGet("/Pages/Auth/accounts")]
    public async Task<IActionResult> Index()
    {
        var accounts = await db.Users.AsNoTracking().Include(u => u.OwnershipDocuments).OrderByDescending(u => u.Id).ToListAsync();
        var memberships = await (from ur in db.UserRoles join r in db.Roles on ur.RoleId equals r.Id
                                 select new { ur.UserId, r.Name }).ToDictionaryAsync(x => x.UserId, x => x.Name);
        return PartialView("_Accounts", accounts.Select(u => new AccountRowViewModel(u.Id, u.FullName, u.Email!, u.Status,
            u.RequestedRole, memberships.GetValueOrDefault(u.Id), u.ConcurrencyStamp!, u.ReviewedAt, u.ReviewedBy,
            u.OwnershipDocuments.Select(d => new AccountDocumentViewModel(d.Id, d.FileName)).ToList())).ToList());
    }

    [HttpPost("/Pages/Auth/accounts/review")]
    public async Task<IActionResult> Review(AccountReviewViewModel input)
    {
        if (!ModelState.IsValid) return ReviewResult("Invalid account review.");
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var user = await db.Users.FromSqlInterpolated($"SELECT * FROM dbo.users WITH (UPDLOCK, HOLDLOCK) WHERE id = {input.Id}").SingleOrDefaultAsync();
        if (user is null || user.Status != "Pending" || user.ConcurrencyStamp != input.ConcurrencyStamp ||
            (await users.GetRolesAsync(user)).Count != 0)
            return ReviewResult("This request has changed. Reload and review it again.");
        if (input.Decision == "approve" && input.AssignedRole == KicksRoles.Owner &&
            !await db.OwnershipDocuments.AnyAsync(d => d.UserId == user.Id))
            return ReviewResult("An ownership document is required before approving a field owner.");
        user.Status = input.Decision == "approve" ? "Active" : "Rejected";
        user.ReviewedAt = DateTimeOffset.UtcNow;
        user.ReviewedBy = int.Parse(users.GetUserId(User)!);
        var result = await users.UpdateAsync(user);
        if (result.Succeeded && input.Decision == "approve") result = await users.AddToRoleAsync(user, input.AssignedRole);
        if (result.Succeeded) result = await users.UpdateSecurityStampAsync(user);
        if (!result.Succeeded) return ReviewResult("The account changed during review. Reload and try again.");
        await transaction.CommitAsync();
        return ReviewResult(input.Decision == "approve" ? "Account approved." : "Registration rejected.");
    }

    [HttpPost("/Pages/Auth/accounts/status")]
    public async Task<IActionResult> Status(AccountStatusViewModel input)
    {
        if (!ModelState.IsValid) return ReviewResult("Invalid account status.");
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var user = await db.Users.FromSqlInterpolated($"SELECT * FROM dbo.users WITH (UPDLOCK, HOLDLOCK) WHERE id = {input.Id}").SingleOrDefaultAsync();
        if (user is null || user.ConcurrencyStamp != input.ConcurrencyStamp ||
            user.Status is not ("Active" or "Inactive")) return ReviewResult("This account has changed. Reload and try again.");
        var roles = await users.GetRolesAsync(user);
        if (roles.Count != 1 || roles[0] is not (KicksRoles.Player or KicksRoles.Owner))
            return ReviewResult("Administrator access cannot be changed through this screen.");
        user.Status = input.Status;
        user.ReviewedBy = int.Parse(users.GetUserId(User)!);
        user.ReviewedAt = DateTimeOffset.UtcNow;
        var result = await users.UpdateAsync(user);
        if (result.Succeeded) result = await users.UpdateSecurityStampAsync(user);
        if (!result.Succeeded) return ReviewResult("The account changed. Reload and try again.");
        await transaction.CommitAsync();
        return ReviewResult("Account access updated. Existing sessions have been revoked.");
    }

    [HttpGet("/Pages/Auth/accounts/documents/{id:int}")]
    public async Task<IActionResult> Document(int id)
    {
        var document = await db.OwnershipDocuments.AsNoTracking().SingleOrDefaultAsync(d => d.Id == id);
        if (document is null) return NotFound();
        var path = documents.PathFor(document.StorageUrl);
        if (!System.IO.File.Exists(path)) return NotFound();
        Response.Headers.XContentTypeOptions = "nosniff";
        return PhysicalFile(path, "application/octet-stream", document.FileName);
    }

    private IActionResult ReviewResult(string message)
    {
        TempData["AccountNotice"] = message;
        return LocalRedirect("/Pages/Dashboard/admin.html#users");
    }
}
