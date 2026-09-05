using Microsoft.EntityFrameworkCore;
using Payment.Application.Common;

namespace Payment.Infrastructure.Persistence;

public sealed class PaymentRepository : IPaymentRepository
{
    private readonly PaymentDbContext _dbContext;

    public PaymentRepository(PaymentDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Payment.Domain.Payment?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken) =>
        _dbContext.Payments.FirstOrDefaultAsync(p => p.OrderId == orderId, cancellationToken);

    public Task<Payment.Domain.Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _dbContext.Payments.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public void Add(Payment.Domain.Payment payment) => _dbContext.Payments.Add(payment);
}
