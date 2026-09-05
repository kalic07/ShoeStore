using System.ComponentModel.DataAnnotations;

namespace Catalog.Api.Dtos;

public sealed record ProductResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Brand { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public string Currency { get; init; } = "USD";
    public string ImageUrl { get; init; } = string.Empty;
    public List<string> AvailableSizes { get; init; } = new();
    public int StockQuantity { get; init; }
    public bool IsActive { get; init; }
}

public sealed record PagedResult<T>
{
    public IReadOnlyCollection<T> Items { get; init; } = Array.Empty<T>();
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
}

public sealed record CreateProductRequest
{
    [Required, MaxLength(200)]
    public string Name { get; init; } = string.Empty;

    [Required, MaxLength(100)]
    public string Brand { get; init; } = string.Empty;

    [MaxLength(2000)]
    public string Description { get; init; } = string.Empty;

    [Required, MaxLength(100)]
    public string Category { get; init; } = string.Empty;

    [Range(0.01, 100000)]
    public decimal Price { get; init; }

    public string ImageUrl { get; init; } = string.Empty;

    public List<string> AvailableSizes { get; init; } = new();

    [Range(0, int.MaxValue)]
    public int StockQuantity { get; init; }
}

public sealed record AdjustStockRequest
{
    /// <summary>Positive to restock, negative to decrement. Result cannot go below zero.</summary>
    public int QuantityDelta { get; init; }
}

/// <summary>Used internally by Order.Api to atomically reserve stock when an order is placed.</summary>
public sealed record ReserveStockRequest
{
    [Range(1, int.MaxValue)]
    public int Quantity { get; init; }
}
