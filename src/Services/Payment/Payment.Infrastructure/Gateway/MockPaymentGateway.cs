using Microsoft.Extensions.Logging;
using Payment.Application.Common;

namespace Payment.Infrastructure.Gateway;

/// <summary>
/// Simulates a real card-network payment gateway (think Stripe/PayPal) so the
/// full checkout -> payment -> order-status flow can be demoed and tested
/// without any external account or network dependency. Swap this out for a
/// real IPaymentGateway implementation (e.g. StripePaymentGateway using the
/// Stripe.net SDK's PaymentIntent API) to go to production - nothing else in
/// the Payment service needs to change.
///
/// Deterministic test outcomes (checked against the token supplied at
/// checkout, case-insensitive):
///   - contains "decline"      -> declined ("Card declined by issuer")
///   - contains "insufficient" -> declined ("Insufficient funds")
///   - contains "error"        -> throws (simulates a gateway/network fault)
///   - anything else           -> approved
/// </summary>
public sealed class MockPaymentGateway : IPaymentGateway
{
    private readonly ILogger<MockPaymentGateway> _logger;

    public MockPaymentGateway(ILogger<MockPaymentGateway> logger)
    {
        _logger = logger;
    }

    public async Task<PaymentGatewayResult> ChargeAsync(string paymentMethodToken, decimal amount, string currency, CancellationToken cancellationToken)
    {
        // Simulate realistic network/processing latency.
        await Task.Delay(TimeSpan.FromMilliseconds(300), cancellationToken);

        var token = paymentMethodToken.ToLowerInvariant();

        if (token.Contains("error"))
        {
            _logger.LogError("Mock gateway simulated a processing error for token {Token}", paymentMethodToken);
            throw new InvalidOperationException("Payment gateway error: could not reach card network.");
        }

        if (token.Contains("insufficient"))
        {
            return new PaymentGatewayResult(false, null, "Insufficient funds.");
        }

        if (token.Contains("decline"))
        {
            return new PaymentGatewayResult(false, null, "Card declined by issuer.");
        }

        var transactionReference = $"mock_txn_{Guid.NewGuid():N}";
        _logger.LogInformation("Mock gateway approved {Amount} {Currency} -> {TransactionReference}", amount, currency, transactionReference);

        return new PaymentGatewayResult(true, transactionReference, null);
    }
}
