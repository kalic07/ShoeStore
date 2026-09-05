using MediatR;

namespace Order.Application.Commands.MarkOrderPaid;

/// <summary>Raised by the RabbitMQ consumer that handles PaymentSucceededIntegrationEvent.</summary>
public sealed record MarkOrderPaidCommand(Guid OrderId, Guid PaymentId) : IRequest;
