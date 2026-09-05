using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Order.Application.Common;

namespace Order.Api.Services;

/// <summary>
/// Reads the authenticated user's id and raw bearer token from the current
/// HTTP request. Registered as Scoped so it's safe to inject into other
/// scoped services (repositories, the Catalog HTTP client) for the lifetime
/// of a single request.
/// </summary>
public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid UserId
    {
        get
        {
            var subClaim = _httpContextAccessor.HttpContext?.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return Guid.TryParse(subClaim, out var id) ? id : Guid.Empty;
        }
    }

    public string? BearerToken
    {
        get
        {
            var header = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
            return header?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) == true
                ? header["Bearer ".Length..]
                : null;
        }
    }
}
