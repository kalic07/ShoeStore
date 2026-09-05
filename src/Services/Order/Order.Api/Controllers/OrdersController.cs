using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Order.Api.Requests;
using Order.Application.Commands.PlaceOrder;
using Order.Application.Common;
using Order.Application.Dtos;
using Order.Application.Queries;

namespace Order.Api.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public sealed class OrdersController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ICurrentUserService _currentUser;

    public OrdersController(ISender sender, ICurrentUserService currentUser)
    {
        _sender = sender;
        _currentUser = currentUser;
    }

    /// <summary>Places a new order (checkout) for the current customer.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<OrderDto>> PlaceOrder([FromBody] PlaceOrderRequest request, CancellationToken cancellationToken)
    {
        var command = new PlaceOrderCommand(
            request.ShippingAddress,
            request.PaymentMethodToken,
            request.Lines.Select(l => new PlaceOrderLine(l.ProductId, l.Quantity)).ToList());

        var order = await _sender.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }

    /// <summary>Gets a single order belonging to the current customer.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<OrderDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var order = await _sender.Send(new GetOrderByIdQuery(id, _currentUser.UserId), cancellationToken);
        return Ok(order);
    }

    /// <summary>Lists the current customer's order history, newest first.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<OrderDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> GetMyOrders(CancellationToken cancellationToken)
    {
        var orders = await _sender.Send(new GetOrdersForCustomerQuery(_currentUser.UserId), cancellationToken);
        return Ok(orders);
    }
}
