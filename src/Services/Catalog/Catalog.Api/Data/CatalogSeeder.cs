using Catalog.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Api.Data;

public static class CatalogSeeder
{
    public static async Task SeedAsync(CatalogDbContext dbContext)
    {
        if (await dbContext.Products.AnyAsync())
        {
            return;
        }

        var sizes = new List<string> { "US 7", "US 8", "US 9", "US 10", "US 11", "US 12" };

        dbContext.Products.AddRange(
            new Product
            {
                Name = "Aero Runner 2",
                Brand = "Velocity",
                Description = "Lightweight everyday running shoe with responsive foam cushioning.",
                Category = "Running",
                Price = 129.99m,
                ImageUrl = "https://images.unsplash.com/photo-1542291026-7eec264c27ff?w=800",
                AvailableSizes = sizes,
                StockQuantity = 84,
            },
            new Product
            {
                Name = "Court Classic Low",
                Brand = "Velocity",
                Description = "Retro low-top sneaker with a durable rubber outsole for everyday wear.",
                Category = "Casual",
                Price = 89.99m,
                ImageUrl = "https://images.unsplash.com/photo-1595950653106-6c9ebd614d3a?w=800",
                AvailableSizes = sizes,
                StockQuantity = 120,
            },
            new Product
            {
                Name = "Hoops Elevate",
                Brand = "Apex",
                Description = "High-top basketball shoe with ankle support and cushioned midsole.",
                Category = "Basketball",
                Price = 159.99m,
                ImageUrl = "https://images.unsplash.com/photo-1600185365483-26d7a4cc7519?w=800",
                AvailableSizes = sizes,
                StockQuantity = 45,
            },
            new Product
            {
                Name = "Trailblazer GTX",
                Brand = "Summit",
                Description = "Waterproof hiking shoe with aggressive tread for technical trails.",
                Category = "Hiking",
                Price = 149.99m,
                ImageUrl = "https://images.unsplash.com/photo-1520639888713-7851133b1ed0?w=800",
                AvailableSizes = sizes,
                StockQuantity = 60,
            },
            new Product
            {
                Name = "Featherlight Trainer",
                Brand = "Apex",
                Description = "Cross-training shoe built for agility work and short sprints.",
                Category = "Training",
                Price = 109.99m,
                ImageUrl = "https://images.unsplash.com/photo-1460353581641-37baddab0fa2?w=800",
                AvailableSizes = sizes,
                StockQuantity = 73,
            },
            new Product
            {
                Name = "Cloudwalk Slip-On",
                Brand = "Summit",
                Description = "Sock-fit slip-on with memory foam insole for all-day comfort.",
                Category = "Casual",
                Price = 79.99m,
                ImageUrl = "https://images.unsplash.com/photo-1549298916-b41d501d3772?w=800",
                AvailableSizes = sizes,
                StockQuantity = 95,
            }
        );

        await dbContext.SaveChangesAsync();
    }
}
