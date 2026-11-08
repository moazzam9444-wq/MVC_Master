using KicksMvc.Data;
using KicksMvc.Models.Entities;
using KicksMvc.Services.Authentication;
using KicksMvc.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace KicksMvc.Controllers;

[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class AuthController(UserManager<User> users, SignInManager<User> signIn,
    KicksDbContext db, OwnershipDocumentStore documents) : Controller
{
    [AllowAnonymous, HttpGet("/Pages/Auth/login.html")]
    public IActionResult Login(string? returnTo = null) => User.Identity?.IsAuthenticated == true
        ? LocalRedirect(AccountAccess.Home(User)) : View(new LoginViewModel { ReturnTo = returnTo });

    [AllowAnonymous, HttpGet("/Pages/Auth/register.html")]
    public IActionResult Register(string? role = null, string? returnTo = null) => User.Identity?.IsAuthenticated == true
        ? LocalRedirect(AccountAccess.Home(User)) : View(new RegisterViewModel { Role = role == "owner" ? "owner" : "player", ReturnTo = returnTo });

    [AllowAnonymous, HttpPost("/Pages/Auth/register.html"), EnableRateLimiting("authentication")]
    [RequestSizeLimit(1200000), RequestFormLimits(MultipartBodyLengthLimit = 1100000)]
    public async Task<IActionResult> Register(RegisterViewModel input)
    {
        if (User.Identity?.IsAuthenticated == true) return Forbid();
        input.FullName = input.FullName?.Trim() ?? "";
        input.Email = input.Email?.Trim() ?? "";
        if (input.FullName.Length < 2) ModelState.AddModelError(nameof(input.FullName), "Enter your full name (at least 2 characters).");
        (byte[] Bytes, string Name, string Mime)? proof = null;
        if (input.Role == "owner")
        {
            if (input.OwnerProof is null) ModelState.AddModelError(nameof(input.OwnerProof), "Upload your field ownership proof / license.");
            else try { proof = await documents.ValidateAsync(input.OwnerProof); }
                catch (InvalidDataException error) { ModelState.AddModelError(nameof(input.OwnerProof), error.Message); }
        }
        if (!ModelState.IsValid) return RegistrationErrors(input);
        string? key = null;
        await using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            var user = new User { FullName = input.FullName, Email = input.Email,
                UserName = input.Email, Status = "Pending", RequestedRole = input.Role, LockoutEnabled = true };
            var result = await users.CreateAsync(user, input.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Code.StartsWith("Duplicate", StringComparison.Ordinal)
                        ? "Unable to register with these details. If you already have an account, sign in or contact KICKS."
                        : error.Description);
                return RegistrationErrors(input);
            }
            if (proof is { } validated)
            {
                key = await documents.SaveAsync(validated.Bytes);
                db.OwnershipDocuments.Add(new OwnershipDocument { UserId = user.Id, FileName = validated.Name,
                    StorageUrl = key, MimeType = validated.Mime, ByteSize = validated.Bytes.Length });
                await db.SaveChangesAsync();
            }
            await transaction.CommitAsync();
        }
        catch (DbUpdateException error) when (error.InnerException is SqlException { Number: 2601 or 2627 })
        {
            if (key is not null) documents.Delete(key);
            ModelState.AddModelError("", "Unable to register with these details. If you already have an account, sign in or contact KICKS.");
            return RegistrationErrors(input);
        }
        catch
        {
            if (key is not null) documents.Delete(key);
            throw;
        }
        TempData["AuthNotice"] = "Request submitted. An administrator will review your information and assign your account role. You can log in once approved.";
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous, HttpPost("/Pages/Auth/login.html"), EnableRateLimiting("authentication")]
    public async Task<IActionResult> Login(LoginViewModel input)
    {
        if (User.Identity?.IsAuthenticated == true) return LocalRedirect(AccountAccess.Home(User));
        if (ModelState.IsValid)
        {
            var user = await users.FindByEmailAsync(input.Email.Trim());
            if (user is not null)
            {
                var result = await signIn.PasswordSignInAsync(user, input.Password, isPersistent: false, lockoutOnFailure: true);
                if (result.Succeeded)
                {
                    var principal = await signIn.CreateUserPrincipalAsync(user);
                    return LocalRedirect(Url.IsLocalUrl(input.ReturnTo) ? input.ReturnTo! : AccountAccess.Home(principal));
                }
            }
            ModelState.AddModelError("", "Unable to sign in. Check your credentials and account approval, or try again later.");
        }
        input.Password = "";
        return View(input);
    }

    [Authorize, HttpPost("/Pages/Auth/logout")]
    public async Task<IActionResult> Logout()
    {
        await signIn.SignOutAsync();
        return LocalRedirect("/index.html");
    }

    [AllowAnonymous, HttpGet("/Pages/Auth/access-denied.html")]
    public IActionResult AccessDenied() { Response.StatusCode = 403; return View(); }

    private IActionResult RegistrationErrors(RegisterViewModel input)
    {
        input.Password = input.ConfirmPassword = "";
        return View("Register", input);
    }
}
