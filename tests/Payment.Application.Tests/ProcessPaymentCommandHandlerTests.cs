using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Payment.Application.Commands.ProcessPayment;
using Payment.Application.Common;
using Shared.Contracts.IntegrationEvents;
using Xunit;

namespace Payment.Application.Tests;

public class ProcessPaymentCommandHandlerTests
{
    private readonly Mock<IPaymentRepository> _paymentRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IPaymentGateway> _gateway = new();
    private readonly Mock<IIntegrationEventPublisher> _eventPublisher = new();

    private ProcessPaymentCommandHandler CreateHandler() => new(
        _paymentRepository.Object, _unitOfWork.Object, _gateway.Object, _eventPublisher.Object,
        NullLogger<ProcessPaymentCommandHandler>.Instance);

    [Fact]
    public async Task Handle_publishes_PaymentSucceeded_when_the_gateway_approves()
    {
        var orderId = Guid.NewGuid();
        _paymentRepository.Setup(r => r.GetByOrderIdAsync(orderId, It.IsAny<CancellationToken>())).ReturnsAsync((Payment.Domain.Payment?)null);
        _gateway.Setup(g => g.ChargeAsync("tok_visa", 100m, "USD", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentGatewayResult(true, "mock_txn_abc", null));

        await CreateHandler().Handle(new ProcessPaymentCommand(orderId, 100m, "USD", "tok_visa"), CancellationToken.None);

        _paymentRepository.Verify(r => r.Add(It.Is<Payment.Domain.Payment>(p => p.Status == Payment.Domain.PaymentStatus.Succeeded)), Times.Once);
        _eventPublisher.Verify(p => p.PublishAsync(
            It.Is<PaymentSucceededIntegrationEvent>(e => e.OrderId == orderId && e.TransactionReference == "mock_txn_abc"),
            It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_publishes_PaymentFailed_when_the_gateway_declines()
    {
        var orderId = Guid.NewGuid();
        _paymentRepository.Setup(r => r.GetByOrderIdAsync(orderId, It.IsAny<CancellationToken>())).ReturnsAsync((Payment.Domain.Payment?)null);
        _gateway.Setup(g => g.ChargeAsync("tok_decline", 100m, "USD", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentGatewayResult(false, null, "Card declined by issuer."));

        await CreateHandler().Handle(new ProcessPaymentCommand(orderId, 100m, "USD", "tok_decline"), CancellationToken.None);

        _paymentRepository.Verify(r => r.Add(It.Is<Payment.Domain.Payment>(p => p.Status == Payment.Domain.PaymentStatus.Declined)), Times.Once);
        _eventPublisher.Verify(p => p.PublishAsync(
            It.Is<PaymentFailedIntegrationEvent>(e => e.OrderId == orderId && e.Reason == "Card declined by issuer."),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_does_not_charge_twice_for_a_redelivered_event()
    {
        var orderId = Guid.NewGuid();
        var existingPayment = Payment.Domain.Payment.CreatePending(orderId, 100m, "USD");
        existingPayment.MarkSucceeded("mock_txn_already_done");

        _paymentRepository.Setup(r => r.GetByOrderIdAsync(orderId, It.IsAny<CancellationToken>())).ReturnsAsync(existingPayment);

        // Simulates RabbitMQ/MassTransit redelivering the same OrderCreated event.
        await CreateHandler().Handle(new ProcessPaymentCommand(orderId, 100m, "USD", "tok_visa"), CancellationToken.None);

        _gateway.Verify(g => g.ChargeAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _paymentRepository.Verify(r => r.Add(It.IsAny<Payment.Domain.Payment>()), Times.Never);

        // But it should still re-announce the (already known) outcome, in case the
        // first PaymentSucceeded publish never reached Order.Api.
        _eventPublisher.Verify(p => p.PublishAsync(
            It.Is<PaymentSucceededIntegrationEvent>(e => e.OrderId == orderId),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
