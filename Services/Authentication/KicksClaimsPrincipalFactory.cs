using System.Security.Claims;
using KicksMvc.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace KicksMvc.Services.Authentication;

// Role-shaped custom user/role claims must never become a second authorization authority.
public sealed class KicksClaimsPrincipalFactory(UserManager<User> users, RoleManager<IdentityRole<int>> roles,
    IOptions<IdentityOptions> options) : UserClaimsPrincipalFactory<User, IdentityRole<int>>(users, roles, options)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(User user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        var roleClaimType = Options.ClaimsIdentity.RoleClaimType;
        foreach (var claim in identity.FindAll(roleClaimType).ToList()) identity.RemoveClaim(claim);
        var membership = await UserManager.GetRolesAsync(user);
        if (membership.Count == 1 && AccountAccess.IsRole(membership[0]))
            identity.AddClaim(new Claim(roleClaimType, membership[0]));
        return identity;
    }
}
