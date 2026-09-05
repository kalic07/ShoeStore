using MediatR;

namespace Order.Application.Commands.MarkOrderPaymentFailed;

/// <summary>Raised by the RabbitMQ consumer that handles PaymentFailedIntegrationEvent.</summary>
public sealed record MarkOrderPaymentFailedCommand(Guid OrderId, string Reason) : IRequest;
