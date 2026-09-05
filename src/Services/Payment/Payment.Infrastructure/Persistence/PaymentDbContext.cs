using MassTransit;
using Microsoft.EntityFrameworkCore;
using PaymentAggregate = Payment.Domain.Payment;

namespace Payment.Infrastructure.Persistence;

public sealed class PaymentDbContext : DbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options) { }

    public DbSet<PaymentAggregate> Payments => Set<PaymentAggregate>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<PaymentAggregate>(entity =>
        {
            entity.ToTable("Payments");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Amount).HasColumnType("numeric(10,2)");
            entity.Property(p => p.Currency).HasMaxLength(3);
            entity.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(p => p.TransactionReference).HasMaxLength(100);
            entity.Property(p => p.FailureReason).HasMaxLength(1000);

            // Enforces "at most one payment per order" at the database level -
            // a second safety net under the application-level idempotency check
            // in ProcessPaymentCommandHandler.
            entity.HasIndex(p => p.OrderId).IsUnique();
        });

        // Transactional outbox - see Order.Infrastructure/Persistence/OrderDbContext.cs
        // for the full rationale; identical pattern here.
        builder.AddInboxStateEntity();
        builder.AddOutboxMessageEntity();
        builder.AddOutboxStateEntity();
    }
}
