namespace Shared.Contracts.IntegrationEvents;

/// <summary>
/// Published by the Payment service after it successfully authorizes and
/// captures payment for an order. Consumed by the Order service to move the
/// order from "Pending" to "Paid".
/// </summary>
public sealed record PaymentSucceededIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();

    public Guid OrderId { get; init; }

    public Guid PaymentId { get; init; }

    public decimal AmountCharged { get; init; }

    public string Currency { get; init; } = "USD";

    /// <summary>Mock gateway authorization/transaction reference.</summary>
    public string TransactionReference { get; init; } = string.Empty;

    public DateTimeOffset OccurredAtUtc { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Published by the Payment service when the mock gateway declines or errors
/// out. Consumed by the Order service to move the order to "PaymentFailed"
/// (compensating action in the saga - the order is not shipped).
/// </summary>
public sealed record PaymentFailedIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();

    public Guid OrderId { get; init; }

    public Guid PaymentId { get; init; }

    public string Reason { get; init; } = string.Empty;

    public DateTimeOffset OccurredAtUtc { get; init; } = DateTimeOffset.UtcNow;
}
