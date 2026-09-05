using MediatR;
using Order.Application.Common;
using Order.Application.Dtos;
using Shared.Contracts.Exceptions;

namespace Order.Application.Queries;

/// <summary>Fetches a single order. <paramref name="RequestingCustomerId"/> enforces
/// that customers can only ever read their own orders (an Admin bypass could be
/// added here later via a role check).</summary>
public sealed record GetOrderByIdQuery(Guid OrderId, Guid RequestingCustomerId) : IRequest<OrderDto>;

public sealed class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, OrderDto>
{
    private readonly IOrderRepository _orderRepository;

    public GetOrderByIdQueryHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<OrderDto> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken)
            ?? throw new NotFoundException("Order", request.OrderId);

        if (order.CustomerId != request.RequestingCustomerId)
        {
            // Deliberately a 404, not a 403 - we don't want to confirm to an
            // attacker that an order id belonging to someone else even exists.
            throw new NotFoundException("Order", request.OrderId);
        }

        return order.ToDto();
    }
}
