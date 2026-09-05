using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Order.Application.Commands.MarkOrderPaid;
using Order.Application.Common;
using Order.Domain;
using Shared.Contracts.Exceptions;
using Xunit;

namespace Order.Application.Tests;

public class MarkOrderPaidCommandHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private MarkOrderPaidCommandHandler CreateHandler() =>
        new(_orderRepository.Object, _unitOfWork.Object, NullLogger<MarkOrderPaidCommandHandler>.Instance);

    private static Order.Domain.Order CreatePendingOrder() => Order.Domain.Order.Place(
        Guid.NewGuid(), "123 Main St", "tok_visa", new[] { new OrderLineDraft(Guid.NewGuid(), "Shoe", 1, 100m) });

    [Fact]
    public async Task Handle_marks_pending_order_as_paid_and_saves()
    {
        var order = CreatePendingOrder();
        var paymentId = Guid.NewGuid();
        _orderRepository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        await CreateHandler().Handle(new MarkOrderPaidCommand(order.Id, paymentId), CancellationToken.None);

        order.Status.Should().Be(OrderStatus.Paid);
        order.PaymentId.Should().Be(paymentId);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_is_idempotent_when_the_event_is_redelivered()
    {
        var order = CreatePendingOrder();
        order.MarkAsPaid(Guid.NewGuid()); // already processed once
        _orderRepository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        // Simulates RabbitMQ/MassTransit's at-least-once delivery redelivering the
        // same PaymentSucceeded event a second time.
        await CreateHandler().Handle(new MarkOrderPaidCommand(order.Id, Guid.NewGuid()), CancellationToken.None);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_throws_not_found_for_unknown_order()
    {
        _orderRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order.Domain.Order?)null);

        var act = async () => await CreateHandler().Handle(new MarkOrderPaidCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
