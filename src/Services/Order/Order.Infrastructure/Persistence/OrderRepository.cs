using Microsoft.EntityFrameworkCore;
using Order.Application.Common;

namespace Order.Infrastructure.Persistence;

public sealed class OrderRepository : IOrderRepository
{
    private readonly OrderDbContext _dbContext;

    public OrderRepository(OrderDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // Note: Items is an owned collection (OwnsMany), which EF Core always loads
    // together with its owner automatically - no explicit .Include() needed.
    public Task<Order.Domain.Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _dbContext.Orders.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Order.Domain.Order>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken) =>
        await _dbContext.Orders
            .Where(o => o.CustomerId == customerId)
            .ToListAsync(cancellationToken);

    public void Add(Order.Domain.Order order) => _dbContext.Orders.Add(order);
}
