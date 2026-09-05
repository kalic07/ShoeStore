using MassTransit;
using Payment.Application.Common;

namespace Payment.Infrastructure.Messaging;

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
