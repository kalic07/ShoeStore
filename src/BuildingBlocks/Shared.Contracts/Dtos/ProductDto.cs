namespace Shared.Contracts.Dtos;

/// <summary>
/// Shared read model for a product, returned by Catalog.Api and consumed by
/// Order.Api (server-side price verification at checkout time - never trust
/// prices sent from the client).
/// </summary>
public sealed record ProductDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Brand { get; init; } = string.Empty;

    public decimal Price { get; init; }

    public string Currency { get; init; } = "USD";

    public int StockQuantity { get; init; }

    public bool IsActive { get; init; }
}
