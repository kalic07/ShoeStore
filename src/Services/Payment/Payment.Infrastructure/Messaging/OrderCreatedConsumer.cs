using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Payment.Application.Commands.ProcessPayment;
using Shared.Contracts.IntegrationEvents;

namespace Payment.Infrastructure.Messaging;

/// <summary>
/// Consumes OrderCreatedIntegrationEvent (published by Order.Api) and drives
/// payment processing. Thin adapter over MediatR, same pattern used on the
/// Order side for the payment-result consumers.
/// </summary>
public sealed class OrderCreatedConsumer : IConsumer<OrderCreatedIntegrationEvent>
{
    private readonly ISender _sender;
    private readonly ILogger<OrderCreatedConsumer> _logger;

    public OrderCreatedConsumer(ISender sender, ILogger<OrderCreatedConsumer> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderCreatedIntegrationEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation("Received OrderCreated for order {OrderId}, charging {Amount} {Currency}", message.OrderId, message.TotalAmount, message.Currency);

        await _sender.Send(
            new ProcessPaymentCommand(message.OrderId, message.TotalAmount, message.Currency, message.PaymentMethodToken),
            context.CancellationToken);
    }
}
