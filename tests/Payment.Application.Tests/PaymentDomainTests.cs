using FluentAssertions;
using Payment.Domain;
using Xunit;

namespace Payment.Application.Tests;

public class PaymentDomainTests
{
    [Fact]
    public void CreatePending_starts_in_pending_status()
    {
        var payment = Payment.Domain.Payment.CreatePending(Guid.NewGuid(), 100m, "USD");

        payment.Status.Should().Be(PaymentStatus.Pending);
    }

    [Fact]
    public void CreatePending_throws_for_non_positive_amount()
    {
        var act = () => Payment.Domain.Payment.CreatePending(Guid.NewGuid(), 0m, "USD");

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void MarkSucceeded_records_the_transaction_reference()
    {
        var payment = Payment.Domain.Payment.CreatePending(Guid.NewGuid(), 100m, "USD");

        payment.MarkSucceeded("mock_txn_123");

        payment.Status.Should().Be(PaymentStatus.Succeeded);
        payment.TransactionReference.Should().Be("mock_txn_123");
        payment.ProcessedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void MarkDeclined_cannot_be_called_twice()
    {
        var payment = Payment.Domain.Payment.CreatePending(Guid.NewGuid(), 100m, "USD");
        payment.MarkDeclined("Card declined by issuer.");

        var act = () => payment.MarkDeclined("Insufficient funds.");

        act.Should().Throw<InvalidOperationException>();
    }
}
