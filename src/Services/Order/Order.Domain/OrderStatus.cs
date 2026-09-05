namespace Order.Domain;

public enum OrderStatus
{
    /// <summary>Order persisted, stock reserved, waiting on the Payment service.</summary>
    Pending = 0,

    /// <summary>Payment.Api confirmed the charge succeeded.</summary>
    Paid = 1,

    /// <summary>Payment.Api reported a decline/error; order will not be fulfilled.</summary>
    PaymentFailed = 2,

    /// <summary>Cancelled by the customer or an admin before payment completed.</summary>
    Cancelled = 3,

    /// <summary>Fulfillment has shipped the order (out of scope for this sample, modeled for completeness).</summary>
    Shipped = 4,
}
