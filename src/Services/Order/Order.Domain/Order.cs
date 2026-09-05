namespace Order.Domain;

/// <summary>Input for a single line when placing a new order.</summary>
public sealed record OrderLineDraft(Guid ProductId, string ProductName, int Quantity, decimal UnitPrice);

/// <summary>
/// Aggregate root for a customer order. All state transitions are exposed as
/// intention-revealing methods (never raw property setters) so invalid
/// transitions - e.g. marking a cancelled order as paid - are impossible by
/// construction rather than something callers have to remember to check.
/// </summary>
public sealed class Order
{
    private readonly List<OrderItem> _items = new();

    public Guid Id { get; private set; }

    public Guid CustomerId { get; private set; }

    public OrderStatus Status { get; private set; }

    public string Currency { get; private set; } = "USD";

    public string ShippingAddress { get; private set; } = string.Empty;

    public string PaymentMethodToken { get; private set; } = string.Empty;

    /// <summary>Set once Payment.Api confirms the charge (via PaymentSucceededIntegrationEvent).</summary>
    public Guid? PaymentId { get; private set; }

    public string? FailureReason { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    public decimal TotalAmount => _items.Sum(i => i.LineTotal);

    private Order() { } // EF Core

    public static Order Place(
        Guid customerId,
        string shippingAddress,
        string paymentMethodToken,
        IEnumerable<OrderLineDraft> lines)
    {
        if (customerId == Guid.Empty) throw new ArgumentException("Customer id is required.", nameof(customerId));
        if (string.IsNullOrWhiteSpace(shippingAddress)) throw new ArgumentException("Shipping address is required.", nameof(shippingAddress));
        if (string.IsNullOrWhiteSpace(paymentMethodToken)) throw new ArgumentException("A payment method is required.", nameof(paymentMethodToken));

        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            ShippingAddress = shippingAddress,
            PaymentMethodToken = paymentMethodToken,
            Status = OrderStatus.Pending,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };

        foreach (var line in lines)
        {
            order._items.Add(new OrderItem(line.ProductId, line.ProductName, line.Quantity, line.UnitPrice));
        }

        if (order._items.Count == 0)
        {
            throw new InvalidOperationException("An order must contain at least one item.");
        }

        return order;
    }

    public void MarkAsPaid(Guid paymentId)
    {
        EnsureTransitionAllowed(OrderStatus.Paid);
        PaymentId = paymentId;
        Status = OrderStatus.Paid;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void MarkPaymentFailed(string reason)
    {
        EnsureTransitionAllowed(OrderStatus.PaymentFailed);
        FailureReason = reason;
        Status = OrderStatus.PaymentFailed;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Cancel()
    {
        if (Status is OrderStatus.Paid or OrderStatus.Shipped)
        {
            throw new InvalidOperationException($"Cannot cancel an order in status '{Status}'.");
        }

        Status = OrderStatus.Cancelled;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    private void EnsureTransitionAllowed(OrderStatus target)
    {
        var allowed = (Status, target) switch
        {
            (OrderStatus.Pending, OrderStatus.Paid) => true,
            (OrderStatus.Pending, OrderStatus.PaymentFailed) => true,
            _ => false,
        };

        if (!allowed)
        {
            throw new InvalidOperationException($"Cannot transition order {Id} from '{Status}' to '{target}'.");
        }
    }
}
