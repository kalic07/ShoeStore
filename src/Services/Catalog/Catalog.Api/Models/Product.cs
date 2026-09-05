namespace Catalog.Api.Models;

public sealed class Product
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string Brand { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    /// <summary>e.g. "Running", "Basketball", "Casual", "Hiking".</summary>
    public string Category { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public string Currency { get; set; } = "USD";

    public string ImageUrl { get; set; } = string.Empty;

    /// <summary>e.g. ["US 7", "US 8", "US 9", "US 10"]. Mapped to a native Postgres text[] column.</summary>
    public List<string> AvailableSizes { get; set; } = new();

    public int StockQuantity { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
