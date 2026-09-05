using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrderAggregate = Order.Domain.Order;

namespace Order.Infrastructure.Persistence;

public sealed class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options) { }

    public DbSet<OrderAggregate> Orders => Set<OrderAggregate>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<OrderAggregate>(entity =>
        {
            entity.ToTable("Orders");
            entity.HasKey(o => o.Id);
            entity.Property(o => o.Status).HasConversion<string>().HasMaxLength(30);
            entity.Property(o => o.Currency).HasMaxLength(3);
            entity.Property(o => o.ShippingAddress).HasMaxLength(500);
            entity.Property(o => o.PaymentMethodToken).HasMaxLength(200);
            entity.Property(o => o.FailureReason).HasMaxLength(1000);
            entity.HasIndex(o => o.CustomerId);

            // OrderItem has no public setters and Order exposes it only as a
            // read-only IReadOnlyCollection<OrderItem> backed by a private
            // `_items` field, so EF Core is told explicitly to materialize the
            // collection through that field (see EF Core "backing fields" docs)
            // rather than trying to use a (non-existent) property setter.
            entity.OwnsMany(o => o.Items, itemBuilder =>
            {
                itemBuilder.ToTable("OrderItems");
                itemBuilder.WithOwner().HasForeignKey(i => i.OrderId);
                itemBuilder.HasKey(i => i.Id);
                itemBuilder.Property(i => i.ProductName).HasMaxLength(200);
                itemBuilder.Property(i => i.UnitPrice).HasColumnType("numeric(10,2)");
            });

            entity.Navigation(o => o.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        // Transactional outbox: MassTransit writes any IPublishEndpoint.Publish()
        // call made in this DbContext's scope into these tables as part of the
        // *same* SaveChanges transaction as the order insert/update above. A
        // background bus-outbox delivery service then reliably drains them to
        // RabbitMQ - so a publish can never "succeed" without the DB write (or
        // vice versa). See Order.Api/Program.cs for the AddEntityFrameworkOutbox /
        // UseBusOutbox registration this depends on.
        builder.AddInboxStateEntity();
        builder.AddOutboxMessageEntity();
        builder.AddOutboxStateEntity();
    }
}
