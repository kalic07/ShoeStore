namespace Shared.Infrastructure.Auth;

/// <summary>
/// Bound from the "Jwt" configuration section. The same signing key/issuer/
/// audience values are shared by Identity.Api (which issues tokens) and
/// every downstream API (which only ever validates them). In production
/// this should be an asymmetric key pair (RS256) managed via a secret
/// store, not a shared symmetric secret in appsettings.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public string SigningKey { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 30;
}
