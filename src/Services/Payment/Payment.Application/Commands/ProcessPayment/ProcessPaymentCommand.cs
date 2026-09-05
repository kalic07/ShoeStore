using MediatR;

namespace Payment.Application.Commands.ProcessPayment;

/// <summary>Raised by the RabbitMQ consumer that handles OrderCreatedIntegrationEvent.</summary>
public sealed record ProcessPaymentCommand(
    Guid OrderId,
    decimal Amount,
    string Currency,
    string PaymentMethodToken) : IRequest;
