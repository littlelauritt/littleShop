using littleShop.orders.Data;
using littleShop.orders.DTOs;
using littleShop.orders.Entities;
using littleShop.orders.Shared;
using Microsoft.EntityFrameworkCore;

namespace littleShop.orders.Services;

public class OrderService(OrdersDbContext db)
{
    public async Task<ServiceResult<OrderResponse>> CreateOrderAsync(string userId, CreateOrderRequest request)
    {
        var order = new Order
        {
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            Items = request.Items.Select(i => new OrderItem
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()
        };

        order.TotalAmount = order.Items.Sum(i => i.Quantity * i.UnitPrice);

        db.Orders.Add(order);
        await db.SaveChangesAsync();

        return ServiceResult<OrderResponse>.Success(MapToResponse(order));
    }

    public async Task<ServiceResult<IEnumerable<OrderResponse>>> GetMyOrdersAsync(string userId)
    {
        var orders = await db.Orders
            .Include(o => o.Items)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        var response = orders.Select(MapToResponse);
        return ServiceResult<IEnumerable<OrderResponse>>.Success(response);
    }

    private static OrderResponse MapToResponse(Order order)
    {
        var items = order.Items.Select(i => new OrderItemDto(i.ProductId, i.ProductName, i.Quantity, i.UnitPrice)).ToList();
        return new OrderResponse(order.Id, order.UserId, order.CreatedAt, order.TotalAmount, items);
    }
}