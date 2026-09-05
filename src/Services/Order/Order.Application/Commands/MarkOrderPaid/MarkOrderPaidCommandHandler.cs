using MediatR;
using Microsoft.Extensions.Logging;
using Order.Application.Common;
using Shared.Contracts.Exceptions;

namespace Order.Application.Commands.MarkOrderPaid;

public sealed class MarkOrderPaidCommandHandler : IRequestHandler<MarkOrderPaidCommand>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MarkOrderPaidCommandHandler> _logger;

    public MarkOrderPaidCommandHandler(IOrderRepository orderRepository, IUnitOfWork unitOfWork, ILogger<MarkOrderPaidCommandHandler> logger)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(MarkOrderPaidCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken)
            ?? throw new NotFoundException("Order", request.OrderId);

        if (order.Status != Order.Domain.OrderStatus.Pending)
        {
            // Idempotency guard: if this event is delivered more than once (at-least-once
            // delivery is the RabbitMQ/MassTransit default), don't double-apply it.
            _logger.LogInformation("Order {OrderId} already in status {Status}; ignoring duplicate PaymentSucceeded event", order.Id, order.Status);
            return;
        }

        order.MarkAsPaid(request.PaymentId);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Order {OrderId} marked as Paid (payment {PaymentId})", order.Id, request.PaymentId);
    }
}
