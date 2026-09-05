using Microsoft.AspNetCore.Identity;

namespace Identity.Api.Models;

/// <summary>
/// Extends the built-in ASP.NET Core Identity user with the extra profile
/// fields a storefront checkout needs. Identity handles password hashing
/// (PBKDF2), lockout-after-N-failed-attempts, and email/username uniqueness
/// for us - avoid ever hand-rolling that logic.
/// </summary>
public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
