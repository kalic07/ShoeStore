using Order.Domain;
using Shared.Contracts.Dtos;

namespace Order.Application.Common;

public interface IOrderRepository
{
    Task<Order.Domain.Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Order.Domain.Order>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken);

    void Add(Order.Domain.Order order);
}

/// <summary>Commits pending changes tracked by <see cref="IOrderRepository"/> in a single transaction.</summary>
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Thin abstraction over the messaging infrastructure so Application code
/// never references MassTransit directly. The Infrastructure implementation
/// forwards to MassTransit's IPublishEndpoint, which - combined with the EF
/// Core transactional outbox configured in Order.Infrastructure - guarantees
/// the event is only published if (and as soon as) the surrounding
/// SaveChangesAsync call actually commits. This avoids the classic
/// dual-write problem (saving to the DB succeeding but the broker publish
/// failing, or vice versa).
/// </summary>
public interface IIntegrationEventPublisher
{
    Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken) where TEvent : class;
}

/// <summary>
/// Calls out to Catalog.Api to verify authoritative pricing/stock at checkout
/// time (never trust a price the client sends) and to reserve inventory.
/// Implemented in Infrastructure using a resilient (Polly-wrapped) HttpClient.
/// </summary>
public interface ICatalogServiceClient
{
    Task<ProductDto?> GetProductAsync(Guid productId, CancellationToken cancellationToken);

    Task ReserveStockAsync(Guid productId, int quantity, CancellationToken cancellationToken);
}

/// <summary>Exposes the calling user's identity, populated from JWT claims by the API layer.</summary>
public interface ICurrentUserService
{
    Guid UserId { get; }

    string? BearerToken { get; }
}
