namespace Shared.Contracts.IntegrationEvents;

/// <summary>
/// Published by the Order service once an order has been persisted in the
/// "Pending" state. Consumed by the Payment service to trigger payment
/// processing. This is the "choreography" trigger in the order/payment saga.
/// </summary>
public sealed record OrderCreatedIntegrationEvent
{
    /// <summary>Unique id for this event instance, used for consumer idempotency/logging.</summary>
    public Guid EventId { get; init; } = Guid.NewGuid();

    public Guid OrderId { get; init; }

    public Guid CustomerId { get; init; }

    public decimal TotalAmount { get; init; }

    public string Currency { get; init; } = "USD";

    /// <summary>Card token / mock payment method reference supplied at checkout.</summary>
    public string PaymentMethodToken { get; init; } = string.Empty;

    public IReadOnlyCollection<OrderItemPayload> Items { get; init; } = Array.Empty<OrderItemPayload>();

    public DateTimeOffset OccurredAtUtc { get; init; } = DateTimeOffset.UtcNow;
}

public sealed record OrderItemPayload
{
    public Guid ProductId { get; init; }

    public string ProductName { get; init; } = string.Empty;

    public int Quantity { get; init; }

    public decimal UnitPrice { get; init; }
}
