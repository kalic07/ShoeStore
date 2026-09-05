using MediatR;
using Order.Application.Dtos;

namespace Order.Application.Commands.PlaceOrder;

public sealed record PlaceOrderLine(Guid ProductId, int Quantity);

/// <summary>
/// Places a new order for the currently authenticated customer. CustomerId is
/// deliberately NOT a field on this command - it is taken from
/// ICurrentUserService inside the handler so a customer can never place an
/// order "as" someone else by tampering with the request body.
/// </summary>
public sealed record PlaceOrderCommand(
    string ShippingAddress,
    string PaymentMethodToken,
    IReadOnlyCollection<PlaceOrderLine> Lines) : IRequest<OrderDto>;
