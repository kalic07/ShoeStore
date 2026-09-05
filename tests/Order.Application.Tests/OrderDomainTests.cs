using FluentAssertions;
using Order.Domain;
using Xunit;

namespace Order.Application.Tests;

public class OrderDomainTests
{
    private static Order.Domain.Order CreateSampleOrder() => Order.Domain.Order.Place(
        customerId: Guid.NewGuid(),
        shippingAddress: "123 Main St",
        paymentMethodToken: "tok_visa",
        lines: new[] { new OrderLineDraft(Guid.NewGuid(), "Aero Runner 2", 2, 129.99m) });

    [Fact]
    public void Place_calculates_total_from_line_items()
    {
        var order = CreateSampleOrder();

        order.TotalAmount.Should().Be(259.98m);
        order.Status.Should().Be(OrderStatus.Pending);
    }

    [Fact]
    public void Place_throws_when_there_are_no_line_items()
    {
        var act = () => Order.Domain.Order.Place(Guid.NewGuid(), "addr", "tok", Array.Empty<OrderLineDraft>());

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void MarkAsPaid_transitions_pending_order_to_paid()
    {
        var order = CreateSampleOrder();
        var paymentId = Guid.NewGuid();

        order.MarkAsPaid(paymentId);

        order.Status.Should().Be(OrderStatus.Paid);
        order.PaymentId.Should().Be(paymentId);
    }

    [Fact]
    public void MarkAsPaid_throws_when_order_is_already_paid()
    {
        var order = CreateSampleOrder();
        order.MarkAsPaid(Guid.NewGuid());

        var act = () => order.MarkAsPaid(Guid.NewGuid());

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Cancel_throws_once_order_has_been_paid()
    {
        var order = CreateSampleOrder();
        order.MarkAsPaid(Guid.NewGuid());

        var act = () => order.Cancel();

        act.Should().Throw<InvalidOperationException>();
    }
}
