using MassTransit;
using Order.Application.Common;

namespace Order.Infrastructure.Messaging;

/// <summary>
/// Forwards to MassTransit's <see cref="IPublishEndpoint"/>. When called from
/// within a request that also uses <see cref="Persistence.OrderDbContext"/>
/// (with the EF outbox configured), MassTransit's outbox interceptor
/// transparently buffers the message into the OutboxMessage table instead of
/// sending it immediately - see OrderDbContext for details.
/// </summary>
public sealed class MassTransitIntegrationEventPublisher : IIntegrationEventPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;

    public MassTransitIntegrationEventPublisher(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken) where TEvent : class =>
        _publishEndpoint.Publish(integrationEvent, cancellationToken);
}
