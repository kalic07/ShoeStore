namespace Order.Application.Dtos;

public static class OrderMappingExtensions
{
    public static OrderDto ToDto(this Order.Domain.Order order) => new()
    {
        Id = order.Id,
        CustomerId = order.CustomerId,
        Status = order.Status.ToString(),
        TotalAmount = order.TotalAmount,
        Currency = order.Currency,
        ShippingAddress = order.ShippingAddress,
        FailureReason = order.FailureReason,
        CreatedAtUtc = order.CreatedAtUtc,
        Items = order.Items.Select(i => new OrderItemDto
        {
            ProductId = i.ProductId,
            ProductName = i.ProductName,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
            LineTotal = i.LineTotal,
        }).ToList(),
    };
}
