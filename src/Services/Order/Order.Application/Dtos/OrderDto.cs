namespace Order.Application.Dtos;

public sealed record OrderDto
{
    public Guid Id { get; init; }
    public Guid CustomerId { get; init; }
    public string Status { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public string Currency { get; init; } = "USD";
    public string ShippingAddress { get; init; } = string.Empty;
    public string? FailureReason { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
    public IReadOnlyCollection<OrderItemDto> Items { get; init; } = Array.Empty<OrderItemDto>();
}

public sealed record OrderItemDto
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal LineTotal { get; init; }
}
