namespace Payment.Domain;

/// <summary>
/// Aggregate root recording one payment attempt against an order. There is
/// exactly one Payment row per OrderId in this sample (see the unique index
/// in PaymentDbContext), which is what makes the RabbitMQ consumer that
/// creates these idempotent under at-least-once delivery.
/// </summary>
public sealed class Payment
{
    public Guid Id { get; private set; }

    public Guid OrderId { get; private set; }

    public decimal Amount { get; private set; }

    public string Currency { get; private set; } = "USD";

    public PaymentStatus Status { get; private set; }

    public string? TransactionReference { get; private set; }

    public string? FailureReason { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? ProcessedAtUtc { get; private set; }

    private Payment() { } // EF Core

    public static Payment CreatePending(Guid orderId, decimal amount, string currency)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "Payment amount must be positive.");

        return new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Amount = amount,
            Currency = currency,
            Status = PaymentStatus.Pending,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
    }

    public void MarkSucceeded(string transactionReference)
    {
        EnsurePending();
        Status = PaymentStatus.Succeeded;
        TransactionReference = transactionReference;
        ProcessedAtUtc = DateTimeOffset.UtcNow;
    }

    public void MarkDeclined(string reason)
    {
        EnsurePending();
        Status = PaymentStatus.Declined;
        FailureReason = reason;
        ProcessedAtUtc = DateTimeOffset.UtcNow;
    }

    private void EnsurePending()
    {
        if (Status != PaymentStatus.Pending)
        {
            throw new InvalidOperationException($"Payment {Id} has already been processed (status '{Status}').");
        }
    }
}
