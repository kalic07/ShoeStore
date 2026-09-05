using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payment.Application.Queries;

namespace Payment.Api.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize]
public sealed class PaymentsController : ControllerBase
{
    private readonly ISender _sender;

    public PaymentsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Looks up the payment outcome for an order. Since payment happens
    /// asynchronously (via the OrderCreated/PaymentSucceeded event flow), the
    /// storefront polls this - or simply polls GET /api/orders/{id} on
    /// Order.Api, which reflects the same outcome once the event round-trip
    /// completes - to show the customer a live status update after checkout.
    /// </summary>
    [HttpGet("order/{orderId:guid}")]
    [ProducesResponseType(typeof(PaymentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaymentDto>> GetByOrderId(Guid orderId, CancellationToken cancellationToken)
    {
        var payment = await _sender.Send(new GetPaymentByOrderIdQuery(orderId), cancellationToken);
        return Ok(payment);
    }
}
