using Catalog.Api.Data;
using Catalog.Api.Dtos;
using Catalog.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts.Exceptions;

namespace Catalog.Api.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsController : ControllerBase
{
    private readonly CatalogDbContext _dbContext;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(CatalogDbContext dbContext, ILogger<ProductsController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>Browse the storefront catalog with optional filtering and paging.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResult<ProductResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ProductResponse>>> GetProducts(
        [FromQuery] string? category,
        [FromQuery] string? brand,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _dbContext.Products.AsNoTracking().Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(p => p.Category == category);
        }

        if (!string.IsNullOrWhiteSpace(brand))
        {
            query = query.Where(p => p.Brand == brand);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p => EF.Functions.ILike(p.Name, $"%{search}%"));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => ToResponse(p))
            .ToListAsync();

        return Ok(new PagedResult<ProductResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
        });
    }

    /// <summary>Gets a single product's full details.</summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ProductResponse>> GetById(Guid id)
    {
        var product = await _dbContext.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new NotFoundException(nameof(Product), id);

        return Ok(ToResponse(product));
    }

    /// <summary>Creates a new product listing. Requires the Admin role.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<ProductResponse>> Create([FromBody] CreateProductRequest request)
    {
        var product = new Product
        {
            Name = request.Name,
            Brand = request.Brand,
            Description = request.Description,
            Category = request.Category,
            Price = request.Price,
            ImageUrl = request.ImageUrl,
            AvailableSizes = request.AvailableSizes,
            StockQuantity = request.StockQuantity,
        };

        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = product.Id }, ToResponse(product));
    }

    /// <summary>Restocks or manually corrects inventory. Requires the Admin role.</summary>
    [HttpPost("{id:guid}/stock/adjust")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ProductResponse>> AdjustStock(Guid id, [FromBody] AdjustStockRequest request)
    {
        var product = await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new NotFoundException(nameof(Product), id);

        var newQuantity = product.StockQuantity + request.QuantityDelta;
        if (newQuantity < 0)
        {
            throw new ConflictException($"Adjustment would result in negative stock ({newQuantity}).");
        }

        product.StockQuantity = newQuantity;
        await _dbContext.SaveChangesAsync();

        return Ok(ToResponse(product));
    }

    /// <summary>
    /// Atomically decrements stock when an order is placed. Called by Order.Api
    /// (with the customer's own bearer token forwarded) as part of checkout, so
    /// two customers can't both "buy" the last pair of shoes. Uses optimistic
    /// concurrency (Postgres xmin) with a few retries under contention.
    /// </summary>
    [HttpPost("{id:guid}/reserve")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ReserveStock(Guid id, [FromBody] ReserveStockRequest request)
    {
        const int maxAttempts = 3;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            var product = await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == id)
                ?? throw new NotFoundException(nameof(Product), id);

            if (product.StockQuantity < request.Quantity)
            {
                throw new ConflictException(
                    $"Insufficient stock for '{product.Name}': requested {request.Quantity}, available {product.StockQuantity}.");
            }

            product.StockQuantity -= request.Quantity;

            try
            {
                await _dbContext.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateConcurrencyException) when (attempt < maxAttempts)
            {
                _logger.LogWarning("Concurrency conflict reserving stock for product {ProductId}, retrying (attempt {Attempt})", id, attempt);
                foreach (var entry in _dbContext.ChangeTracker.Entries())
                {
                    entry.State = EntityState.Detached;
                }
            }
        }

        throw new ConflictException($"Could not reserve stock for product '{id}' due to concurrent updates. Please retry.");
    }

    private static ProductResponse ToResponse(Product p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Brand = p.Brand,
        Description = p.Description,
        Category = p.Category,
        Price = p.Price,
        Currency = p.Currency,
        ImageUrl = p.ImageUrl,
        AvailableSizes = p.AvailableSizes,
        StockQuantity = p.StockQuantity,
        IsActive = p.IsActive,
    };
}
