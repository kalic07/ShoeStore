using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Order.Application.Commands.PlaceOrder;
using Order.Application.Common;
using Shared.Contracts.Dtos;
using Shared.Contracts.Exceptions;
using Xunit;

namespace Order.Application.Tests;

public class PlaceOrderCommandHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICatalogServiceClient> _catalogClient = new();
    private readonly Mock<IIntegrationEventPublisher> _eventPublisher = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Guid _customerId = Guid.NewGuid();

    private PlaceOrderCommandHandler CreateHandler()
    {
        _currentUser.SetupGet(c => c.UserId).Returns(_customerId);

        return new PlaceOrderCommandHandler(
            _orderRepository.Object,
            _unitOfWork.Object,
            _catalogClient.Object,
            _eventPublisher.Object,
            _currentUser.Object,
            NullLogger<PlaceOrderCommandHandler>.Instance);
    }

    private static ProductDto SampleProduct(Guid id, decimal price = 129.99m, bool isActive = true) => new()
    {
        Id = id,
        Name = "Aero Runner 2",
        Brand = "Velocity",
        Price = price,
        Currency = "USD",
        StockQuantity = 10,
        IsActive = isActive,
    };

    [Fact]
    public async Task Handle_prices_the_order_from_catalog_data_not_the_client_request()
    {
        var productId = Guid.NewGuid();
        _catalogClient.Setup(c => c.GetProductAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SampleProduct(productId, price: 129.99m));

        var handler = CreateHandler();
        var command = new PlaceOrderCommand("123 Main St", "tok_visa", new[] { new PlaceOrderLine(productId, 2) });

        var result = await handler.Handle(command, CancellationToken.None);

        result.TotalAmount.Should().Be(259.98m);
        result.CustomerId.Should().Be(_customerId);
        _orderRepository.Verify(r => r.Add(It.IsAny<Order.Domain.Order>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_reserves_stock_for_every_line_before_saving_the_order()
    {
        var productId = Guid.NewGuid();
        _catalogClient.Setup(c => c.GetProductAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SampleProduct(productId));

        var handler = CreateHandler();
        var command = new PlaceOrderCommand("123 Main St", "tok_visa", new[] { new PlaceOrderLine(productId, 3) });

        await handler.Handle(command, CancellationToken.None);

        _catalogClient.Verify(c => c.ReserveStockAsync(productId, 3, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_throws_not_found_when_product_does_not_exist()
    {
        var productId = Guid.NewGuid();
        _catalogClient.Setup(c => c.GetProductAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductDto?)null);

        var handler = CreateHandler();
        var command = new PlaceOrderCommand("123 Main St", "tok_visa", new[] { new PlaceOrderLine(productId, 1) });

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _orderRepository.Verify(r => r.Add(It.IsAny<Order.Domain.Order>()), Times.Never);
    }

    [Fact]
    public async Task Handle_throws_conflict_when_product_is_inactive()
    {
        var productId = Guid.NewGuid();
        _catalogClient.Setup(c => c.GetProductAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SampleProduct(productId, isActive: false));

        var handler = CreateHandler();
        var command = new PlaceOrderCommand("123 Main St", "tok_visa", new[] { new PlaceOrderLine(productId, 1) });

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        _catalogClient.Verify(c => c.ReserveStockAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_publishes_OrderCreated_integration_event()
    {
        var productId = Guid.NewGuid();
        _catalogClient.Setup(c => c.GetProductAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SampleProduct(productId));

        var handler = CreateHandler();
        var command = new PlaceOrderCommand("123 Main St", "tok_visa", new[] { new PlaceOrderLine(productId, 1) });

        await handler.Handle(command, CancellationToken.None);

        _eventPublisher.Verify(p => p.PublishAsync(
            It.Is<Shared.Contracts.IntegrationEvents.OrderCreatedIntegrationEvent>(e => e.CustomerId == _customerId),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
