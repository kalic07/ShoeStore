using MediatR;
using Microsoft.Extensions.Logging;
using Order.Application.Common;
using Order.Application.Dtos;
using Order.Domain;
using Shared.Contracts.Exceptions;
using Shared.Contracts.IntegrationEvents;

namespace Order.Application.Commands.PlaceOrder;

public sealed class PlaceOrderCommandHandler : IRequestHandler<PlaceOrderCommand, OrderDto>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICatalogServiceClient _catalogServiceClient;
    private readonly IIntegrationEventPublisher _eventPublisher;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<PlaceOrderCommandHandler> _logger;

    public PlaceOrderCommandHandler(
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork,
        ICatalogServiceClient catalogServiceClient,
        IIntegrationEventPublisher eventPublisher,
        ICurrentUserService currentUser,
        ILogger<PlaceOrderCommandHandler> logger)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
        _catalogServiceClient = catalogServiceClient;
        _eventPublisher = eventPublisher;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<OrderDto> Handle(PlaceOrderCommand request, CancellationToken cancellationToken)
    {
        var lines = new List<OrderLineDraft>();

        // Re-price and re-validate every line against Catalog.Api's authoritative
        // data. We never trust a price the client might send - only product id +
        // quantity - and we reserve stock synchronously so checkout fails fast
        // (with a clear 409) instead of silently overselling.
        foreach (var line in request.Lines)
        {
            var product = await _catalogServiceClient.GetProductAsync(line.ProductId, cancellationToken)
                ?? throw new NotFoundException("Product", line.ProductId);

            if (!product.IsActive)
            {
                throw new ConflictException($"Product '{product.Name}' is no longer available.");
            }

            await _catalogServiceClient.ReserveStockAsync(product.Id, line.Quantity, cancellationToken);

            lines.Add(new OrderLineDraft(product.Id, product.Name, line.Quantity, product.Price));
        }

        var order = Order.Domain.Order.Place(
            customerId: _currentUser.UserId,
            shippingAddress: request.ShippingAddress,
            paymentMethodToken: request.PaymentMethodToken,
            lines: lines);

        _orderRepository.Add(order);

        // Publish OrderCreated in the SAME unit of work as the insert below via
        // the EF Core transactional outbox (see Order.Infrastructure/OrderDbContext
        // and Program.cs's AddEntityFrameworkOutbox/UseBusOutbox wiring). Either
        // both the order row and the outbox row commit, or neither does - the
        // message can never be "lost" between saving the order and notifying
        // Payment.Api, even if RabbitMQ is briefly unreachable.
        await _eventPublisher.PublishAsync(new OrderCreatedIntegrationEvent
        {
            OrderId = order.Id,
            CustomerId = order.CustomerId,
            TotalAmount = order.TotalAmount,
            Currency = order.Currency,
            PaymentMethodToken = order.PaymentMethodToken,
            Items = order.Items.Select(i => new OrderItemPayload
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
            }).ToList(),
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Order {OrderId} placed for customer {CustomerId}, total {Total} {Currency}",
            order.Id, order.CustomerId, order.TotalAmount, order.Currency);

        return order.ToDto();
    }
}
