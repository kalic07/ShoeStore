using MediatR;
using Microsoft.Extensions.Logging;
using Payment.Application.Common;
using Shared.Contracts.IntegrationEvents;

namespace Payment.Application.Commands.ProcessPayment;

public sealed class ProcessPaymentCommandHandler : IRequestHandler<ProcessPaymentCommand>
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPaymentGateway _paymentGateway;
    private readonly IIntegrationEventPublisher _eventPublisher;
    private readonly ILogger<ProcessPaymentCommandHandler> _logger;

    public ProcessPaymentCommandHandler(
        IPaymentRepository paymentRepository,
        IUnitOfWork unitOfWork,
        IPaymentGateway paymentGateway,
        IIntegrationEventPublisher eventPublisher,
        ILogger<ProcessPaymentCommandHandler> logger)
    {
        _paymentRepository = paymentRepository;
        _unitOfWork = unitOfWork;
        _paymentGateway = paymentGateway;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task Handle(ProcessPaymentCommand request, CancellationToken cancellationToken)
    {
        // Idempotency guard: OrderCreatedIntegrationEvent may be redelivered
        // (RabbitMQ/MassTransit default is at-least-once). If we already have a
        // payment record for this order, don't charge it a second time - just
        // make sure Order.Api has heard the outcome and stop.
        var existing = await _paymentRepository.GetByOrderIdAsync(request.OrderId, cancellationToken);
        if (existing is not null)
        {
            _logger.LogInformation("Payment for order {OrderId} already processed (status {Status}); skipping duplicate charge", request.OrderId, existing.Status);
            await RepublishOutcomeAsync(existing, cancellationToken);
            return;
        }

        var payment = Payment.Domain.Payment.CreatePending(request.OrderId, request.Amount, request.Currency);

        var result = await _paymentGateway.ChargeAsync(request.PaymentMethodToken, request.Amount, request.Currency, cancellationToken);

        if (result.IsSuccess)
        {
            payment.MarkSucceeded(result.TransactionReference!);
        }
        else
        {
            payment.MarkDeclined(result.DeclineReason ?? "Payment declined.");
        }

        _paymentRepository.Add(payment);

        // Same transactional-outbox pattern as Order.Api: publishing here and
        // saving the Payment row happen atomically (see PaymentDbContext).
        await RepublishOutcomeAsync(payment, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Payment {PaymentId} for order {OrderId} processed: {Status}", payment.Id, payment.OrderId, payment.Status);
    }

    private async Task RepublishOutcomeAsync(Payment.Domain.Payment payment, CancellationToken cancellationToken)
    {
        if (payment.Status == Payment.Domain.PaymentStatus.Succeeded)
        {
            await _eventPublisher.PublishAsync(new PaymentSucceededIntegrationEvent
            {
                OrderId = payment.OrderId,
                PaymentId = payment.Id,
                AmountCharged = payment.Amount,
                Currency = payment.Currency,
                TransactionReference = payment.TransactionReference ?? string.Empty,
            }, cancellationToken);
        }
        else if (payment.Status == Payment.Domain.PaymentStatus.Declined)
        {
            await _eventPublisher.PublishAsync(new PaymentFailedIntegrationEvent
            {
                OrderId = payment.OrderId,
                PaymentId = payment.Id,
                Reason = payment.FailureReason ?? "Payment declined.",
            }, cancellationToken);
        }
    }
}
