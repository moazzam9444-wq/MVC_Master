using System.ComponentModel.DataAnnotations;
using System.Text;
using KicksMvc.Data;
using KicksMvc.Models.Entities;
using KicksMvc.Models.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KicksMvc.Services.Authentication;

public static class DevelopmentAdminProvisioning
{
    public static async Task<int> RunAsync(IServiceProvider services, IWebHostEnvironment environment)
    {
        if (!environment.IsDevelopment() || Console.IsInputRedirected)
        {
            Console.Error.WriteLine("Administrator provisioning requires Development and an interactive local terminal.");
            return 1;
        }
        Console.Write("Administrator full name: ");
        var name = Console.ReadLine()?.Trim() ?? "";
        Console.Write("Administrator email: ");
        var email = Console.ReadLine()?.Trim() ?? "";
        if (name.Length is < 2 or > 100 || email.Length > 254 || !new EmailAddressAttribute().IsValid(email))
        { Console.Error.WriteLine("Enter a full name and valid email."); return 1; }
        Console.Write("Password (hidden): ");
        var password = ReadPassword();
        Console.Write("Repeat password (hidden): ");
        if (ReadPassword() != password) { Console.Error.WriteLine("Passwords do not match."); return 1; }
        Console.Write("Type CREATE ADMIN to authorize this account: ");
        if (Console.ReadLine() != "CREATE ADMIN") return 1;
        using var scope = services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var db = scope.ServiceProvider.GetRequiredService<KicksDbContext>();
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        if (await db.UserRoles.AnyAsync(ur => db.Roles.Any(r => r.Id == ur.RoleId && r.Name == KicksRoles.Admin)))
        { Console.Error.WriteLine("An administrator already exists. This command provisions only the first administrator."); return 1; }
        var user = new User { FullName = name, Email = email, UserName = email, Status = "Active", LockoutEnabled = true };
        var result = await users.CreateAsync(user, password);
        if (result.Succeeded) result = await users.AddToRoleAsync(user, KicksRoles.Admin);
        if (!result.Succeeded)
        { foreach (var error in result.Errors) Console.Error.WriteLine(error.Description); return 1; }
        await transaction.CommitAsync();
        Console.WriteLine("First administrator created. Sign in through the normal KICKS login page.");
        return 0;
    }

    private static string ReadPassword()
    {
        var value = new StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter) { Console.WriteLine(); return value.ToString(); }
            if (key.Key == ConsoleKey.Backspace) { if (value.Length > 0) value.Length--; }
            else if (!char.IsControl(key.KeyChar) && value.Length < 129) value.Append(key.KeyChar);
        }
    }
}
