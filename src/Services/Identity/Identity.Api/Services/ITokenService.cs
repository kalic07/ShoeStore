using Identity.Api.Models;

namespace Identity.Api.Services;

public interface ITokenService
{
    (string Token, DateTimeOffset ExpiresAtUtc) CreateAccessToken(ApplicationUser user, IList<string> roles);
}
