using MediatR;
using Order.Application.Common;
using Order.Application.Dtos;

namespace Order.Application.Queries;

public sealed record GetOrdersForCustomerQuery(Guid CustomerId) : IRequest<IReadOnlyList<OrderDto>>;

public sealed class GetOrdersForCustomerQueryHandler : IRequestHandler<GetOrdersForCustomerQuery, IReadOnlyList<OrderDto>>
{
    private readonly IOrderRepository _orderRepository;

    public GetOrdersForCustomerQueryHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<IReadOnlyList<OrderDto>> Handle(GetOrdersForCustomerQuery request, CancellationToken cancellationToken)
    {
        var orders = await _orderRepository.GetByCustomerAsync(request.CustomerId, cancellationToken);
        return orders
            .OrderByDescending(o => o.CreatedAtUtc)
            .Select(o => o.ToDto())
            .ToList();
    }
}
