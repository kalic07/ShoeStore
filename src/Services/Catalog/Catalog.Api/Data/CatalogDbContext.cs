using Catalog.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Data;

public sealed class CatalogDbContext : DbContext
{
    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<Product>(entity =>
        {
            entity.ToTable("Products");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).IsRequired().HasMaxLength(200);
            entity.Property(p => p.Brand).IsRequired().HasMaxLength(100);
            entity.Property(p => p.Category).IsRequired().HasMaxLength(100);
            entity.Property(p => p.Price).HasColumnType("numeric(10,2)");
            entity.HasIndex(p => p.Category);
            entity.HasIndex(p => p.Brand);

            // Use Postgres' native xmin system column as the EF Core concurrency
            // token (as a shadow property - no real column needed) instead of a
            // hand-maintained rowversion column. This is the idiomatic Npgsql way
            // to get optimistic concurrency for free, so a stock decrement can't
            // silently clobber a concurrent one (DbUpdateConcurrencyException is
            // thrown instead).
            entity.UseXminAsConcurrencyToken();
        });
    }
}
