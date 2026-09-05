using MediatR;
using Microsoft.Extensions.Logging;
using Order.Application.Common;
using Shared.Contracts.Exceptions;

namespace Order.Application.Commands.MarkOrderPaymentFailed;

public sealed class MarkOrderPaymentFailedCommandHandler : IRequestHandler<MarkOrderPaymentFailedCommand>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MarkOrderPaymentFailedCommandHandler> _logger;

    public MarkOrderPaymentFailedCommandHandler(IOrderRepository orderRepository, IUnitOfWork unitOfWork, ILogger<MarkOrderPaymentFailedCommandHandler> logger)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(MarkOrderPaymentFailedCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken)
            ?? throw new NotFoundException("Order", request.OrderId);

        if (order.Status != Order.Domain.OrderStatus.Pending)
        {
            _logger.LogInformation("Order {OrderId} already in status {Status}; ignoring duplicate PaymentFailed event", order.Id, order.Status);
            return;
        }

        order.MarkPaymentFailed(request.Reason);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogWarning("Order {OrderId} marked as PaymentFailed: {Reason}", order.Id, request.Reason);
    }
}
