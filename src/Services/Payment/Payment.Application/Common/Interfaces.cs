namespace Payment.Application.Common;

public interface IPaymentRepository
{
    Task<Payment.Domain.Payment?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken);

    Task<Payment.Domain.Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    void Add(Payment.Domain.Payment payment);
}

public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

/// <summary>Same rationale as Order.Application.Common.IIntegrationEventPublisher.</summary>
public interface IIntegrationEventPublisher
{
    Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken) where TEvent : class;
}

public sealed record PaymentGatewayResult(bool IsSuccess, string? TransactionReference, string? DeclineReason);

/// <summary>
/// Abstraction over the payment provider. MockPaymentGateway (Infrastructure)
/// simulates authorize+capture with deterministic outcomes so the checkout
/// flow can be demoed and tested end-to-end without a real Stripe/PayPal
/// account. Swapping in a real provider later means writing one new class
/// behind this interface - nothing in Application or Api changes.
/// </summary>
public interface IPaymentGateway
{
    Task<PaymentGatewayResult> ChargeAsync(string paymentMethodToken, decimal amount, string currency, CancellationToken cancellationToken);
}
