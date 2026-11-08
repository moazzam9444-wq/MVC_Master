using KicksMvc.Models.Entities;
using Microsoft.AspNetCore.Identity;

namespace KicksMvc.Services.Authentication;

public sealed class LetterPasswordValidator : IPasswordValidator<User>
{
    public Task<IdentityResult> ValidateAsync(UserManager<User> manager, User user, string? password) =>
        Task.FromResult(password is not null && password.Any(c => c is >= 'a' and <= 'z' or >= 'A' and <= 'Z') && password.Length <= 128
            ? IdentityResult.Success : IdentityResult.Failed(new IdentityError
            { Code = "PasswordLetter", Description = "Use 8–128 characters, including a letter and a number." }));
}
