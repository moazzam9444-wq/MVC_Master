using KicksMvc.Models.Entities;
using KicksMvc.Models.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace KicksMvc.Services.Authentication;

public static class AccountAccess
{
    public static bool IsRole(string role) => role is KicksRoles.Player or KicksRoles.Owner or KicksRoles.Admin;
    public static async Task<bool> IsActiveAsync(User user, UserManager<User> users)
    {
        if (user.Status != "Active" || await users.IsLockedOutAsync(user)) return false;
        var roles = await users.GetRolesAsync(user);
        return roles.Count == 1 && IsRole(roles[0]);
    }

    public static async Task ValidateCookieAsync(CookieValidatePrincipalContext context)
    {
        await SecurityStampValidator.ValidatePrincipalAsync(context);
        if (context.Principal?.Identity?.IsAuthenticated != true) return;
        var users = context.HttpContext.RequestServices.GetRequiredService<UserManager<User>>();
        var user = await users.GetUserAsync(context.Principal);
        if (user is not null && await IsActiveAsync(user, users)) return;
        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
    }

    public static string Home(ClaimsPrincipal user) => user.IsInRole(KicksRoles.Admin)
        ? "/Pages/Dashboard/admin.html" : user.IsInRole(KicksRoles.Owner)
        ? "/Pages/Dashboard/owner.html" : "/index.html";
}

public sealed class KicksSignInManager(UserManager<User> users, IHttpContextAccessor accessor,
    IUserClaimsPrincipalFactory<User> factory, IOptions<IdentityOptions> options,
    ILogger<SignInManager<User>> logger, IAuthenticationSchemeProvider schemes,
    IUserConfirmation<User> confirmation)
    : SignInManager<User>(users, accessor, factory, options, logger, schemes, confirmation)
{
    public override async Task<bool> CanSignInAsync(User user) =>
        await base.CanSignInAsync(user) && await AccountAccess.IsActiveAsync(user, UserManager);
}
