namespace Order.Api.Requests;

public sealed record PlaceOrderLineRequest(Guid ProductId, int Quantity);

public sealed record PlaceOrderRequest
{
    public string ShippingAddress { get; init; } = string.Empty;

    /// <summary>Opaque token from the storefront's checkout form (mock gateway "card token").</summary>
    public string PaymentMethodToken { get; init; } = string.Empty;

    public List<PlaceOrderLineRequest> Lines { get; init; } = new();
}
