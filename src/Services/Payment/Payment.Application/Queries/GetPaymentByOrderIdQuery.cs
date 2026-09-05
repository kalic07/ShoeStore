using MediatR;
using Payment.Application.Common;
using Shared.Contracts.Exceptions;

namespace Payment.Application.Queries;

public sealed record PaymentDto
{
    public Guid Id { get; init; }
    public Guid OrderId { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "USD";
    public string Status { get; init; } = string.Empty;
    public string? TransactionReference { get; init; }
    public string? FailureReason { get; init; }
}

public sealed record GetPaymentByOrderIdQuery(Guid OrderId) : IRequest<PaymentDto>;

public sealed class GetPaymentByOrderIdQueryHandler : IRequestHandler<GetPaymentByOrderIdQuery, PaymentDto>
{
    private readonly IPaymentRepository _paymentRepository;

    public GetPaymentByOrderIdQueryHandler(IPaymentRepository paymentRepository)
    {
        _paymentRepository = paymentRepository;
    }

    public async Task<PaymentDto> Handle(GetPaymentByOrderIdQuery request, CancellationToken cancellationToken)
    {
        var payment = await _paymentRepository.GetByOrderIdAsync(request.OrderId, cancellationToken)
            ?? throw new NotFoundException("Payment for order", request.OrderId);

        return new PaymentDto
        {
            Id = payment.Id,
            OrderId = payment.OrderId,
            Amount = payment.Amount,
            Currency = payment.Currency,
            Status = payment.Status.ToString(),
            TransactionReference = payment.TransactionReference,
            FailureReason = payment.FailureReason,
        };
    }
}
