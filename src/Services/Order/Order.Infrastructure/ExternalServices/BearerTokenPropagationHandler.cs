using System.Net.Http.Headers;
using Order.Application.Common;

namespace Order.Infrastructure.ExternalServices;

/// <summary>
/// Forwards the calling customer's own JWT to Catalog.Api so the stock
/// "reserve" endpoint's [Authorize] check passes and audit logs downstream
/// can attribute the reservation to a real user, rather than minting a
/// separate service-account token for this one call.
/// </summary>
public sealed class BearerTokenPropagationHandler : DelegatingHandler
{
    private readonly ICurrentUserService _currentUser;

    public BearerTokenPropagationHandler(ICurrentUserService currentUser)
    {
        _currentUser = currentUser;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_currentUser.BearerToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _currentUser.BearerToken);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
