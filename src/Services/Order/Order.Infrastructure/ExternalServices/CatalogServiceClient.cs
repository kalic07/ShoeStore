using System.Net;
using System.Net.Http.Json;
using Order.Application.Common;
using Shared.Contracts.Dtos;
using Shared.Contracts.Exceptions;

namespace Order.Infrastructure.ExternalServices;

/// <summary>
/// HTTP client for Catalog.Api. Wrapped by a Polly retry + circuit-breaker
/// policy (see InfrastructureServiceCollectionExtensions) so a brief blip in
/// Catalog.Api doesn't fail checkout outright.
/// </summary>
public sealed class CatalogServiceClient : ICatalogServiceClient
{
    private readonly HttpClient _httpClient;

    public CatalogServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ProductDto?> GetProductAsync(Guid productId, CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync($"/api/products/{productId}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProductDto>(cancellationToken: cancellationToken);
    }

    public async Task ReserveStockAsync(Guid productId, int quantity, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"/api/products/{productId}/reserve",
            new { Quantity = quantity },
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            var problem = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new ConflictException(string.IsNullOrWhiteSpace(problem)
                ? $"Could not reserve {quantity} unit(s) of product '{productId}'."
                : problem);
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new NotFoundException("Product", productId);
        }

        response.EnsureSuccessStatusCode();
    }
}
