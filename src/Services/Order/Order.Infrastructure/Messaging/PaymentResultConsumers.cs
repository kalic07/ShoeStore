using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Order.Application.Commands.MarkOrderPaid;
using Order.Application.Commands.MarkOrderPaymentFailed;
using Shared.Contracts.IntegrationEvents;

namespace Order.Infrastructure.Messaging;

/// <summary>
/// Consumes PaymentSucceededIntegrationEvent (published by Payment.Api) and
/// translates it into the MarkOrderPaidCommand MediatR request. Kept as a
/// thin adapter - all real logic (idempotency check, state transition) lives
/// in the command handler so it's unit-testable without a message broker.
/// </summary>
public sealed class PaymentSucceededConsumer : IConsumer<PaymentSucceededIntegrationEvent>
{
    private readonly ISender _sender;
    private readonly ILogger<PaymentSucceededConsumer> _logger;

    public PaymentSucceededConsumer(ISender sender, ILogger<PaymentSucceededConsumer> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PaymentSucceededIntegrationEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation("Received PaymentSucceeded for order {OrderId} (payment {PaymentId})", message.OrderId, message.PaymentId);

        await _sender.Send(new MarkOrderPaidCommand(message.OrderId, message.PaymentId), context.CancellationToken);
    }
}

public sealed class PaymentFailedConsumer : IConsumer<PaymentFailedIntegrationEvent>
{
    private readonly ISender _sender;
    private readonly ILogger<PaymentFailedConsumer> _logger;

    public PaymentFailedConsumer(ISender sender, ILogger<PaymentFailedConsumer> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PaymentFailedIntegrationEvent> context)
    {
        var message = context.Message;
        _logger.LogWarning("Received PaymentFailed for order {OrderId}: {Reason}", message.OrderId, message.Reason);

        await _sender.Send(new MarkOrderPaymentFailedCommand(message.OrderId, message.Reason), context.CancellationToken);
    }
}
