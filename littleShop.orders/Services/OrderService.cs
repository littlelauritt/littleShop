using littleShop.orders.Data;
using littleShop.orders.DTOs;
using littleShop.orders.Entities;
using littleShop.orders.Shared;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;
using MassTransit;
using littleShop.Shared.Events;

namespace littleShop.orders.Services;

public class OrderService(
    OrdersDbContext db,
    IHttpClientFactory clientFactory,
    IPublishEndpoint publishEndpoint)
{
    // 1. CREAR PEDIDO
    public async Task<ServiceResult<OrderResponse>> CreateOrderAsync(string userId, string userEmail, CreateOrderRequest request)
    {
        // A. Validar Stock
        var catalogClient = clientFactory.CreateClient("catalog-api");
        foreach (var item in request.Items)
        {
            var response = await catalogClient.PostAsJsonAsync(
                $"/api/v1/products/{item.ProductId}/reduce-stock",
                new { Stock = item.Quantity }
            );

            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return ServiceResult<OrderResponse>.Failure($"Stock insuficiente: {content}");
            }
        }

        // B. Guardar
        var order = new Order
        {
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            Status = OrderStatus.Confirmed,
            CustomerEmail = userEmail,
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

        // C. Evento
        _ = Task.Run(async () =>
        {
            try
            {
                await publishEndpoint.Publish(new OrderCreatedEvent(
                    order.Id,
                    userId,
                    userEmail,
                    order.TotalAmount,
                    order.CreatedAt
                ));
            }
            catch { }
        });

        return ServiceResult<OrderResponse>.Success(MapToResponse(order));
    }

    // 2. ENVIAR PEDIDO
    public async Task<ServiceResult> ShipOrderAsync(int orderId)
    {
        var order = await db.Orders.FindAsync(orderId);
        if (order is null) return ServiceResult.Failure("Pedido no encontrado");

        if (order.Status != OrderStatus.Confirmed && order.Status != OrderStatus.Pending)
            return ServiceResult.Failure("Solo se envían pedidos confirmados o pendientes");

        order.Status = OrderStatus.Shipped;
        await db.SaveChangesAsync();

        _ = Task.Run(async () =>
        {
            try
            {
                await publishEndpoint.Publish(new OrderShippedEvent(
                    order.Id,
                    order.CustomerEmail ?? "sin-email@littleshop.local",
                    "ENVIO-12345"
                ));
            }
            catch { }
        });

        return ServiceResult.Success();
    }

    // 3. CANCELAR (Usuario)
    public async Task<ServiceResult> CancelOrderAsync(int orderId, string userId)
    {
        var order = await db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order is null) return ServiceResult.Failure("Pedido no encontrado");
        if (order.UserId != userId) return ServiceResult.Failure("No tienes permiso");
        if (order.Status == OrderStatus.Cancelled) return ServiceResult.Failure("Ya estaba cancelado");

        order.Status = OrderStatus.Cancelled;
        await db.SaveChangesAsync();

        var itemsToRestore = order.Items.ToDictionary(i => i.ProductId, i => i.Quantity);

        _ = Task.Run(async () =>
        {
            try
            {
                await publishEndpoint.Publish(new OrderCancelledEvent(
                    order.Id,
                    order.CustomerEmail,
                    "Cancelado por el Usuario",
                    itemsToRestore
                ));
            }
            catch { }
        });

        return ServiceResult.Success();
    }

    // 4. CANCELAR (Admin)
    public async Task<ServiceResult> CancelOrderAdminAsync(int orderId)
    {
        var order = await db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order is null) return ServiceResult.Failure("Pedido no encontrado");
        if (order.Status == OrderStatus.Cancelled) return ServiceResult.Failure("Ya estaba cancelado");

        order.Status = OrderStatus.Cancelled;
        await db.SaveChangesAsync();

        var itemsToRestore = order.Items.ToDictionary(i => i.ProductId, i => i.Quantity);

        _ = Task.Run(async () =>
        {
            await publishEndpoint.Publish(new OrderCancelledEvent(
                order.Id,
                order.CustomerEmail,
                "Cancelado por el Admin",
                itemsToRestore
            ));
        });

        return ServiceResult.Success();
    }

    // 5. GET MIS PEDIDOS
    public async Task<ServiceResult<IEnumerable<OrderResponse>>> GetMyOrdersAsync(string userId)
    {
        var orders = await db.Orders
            .Include(o => o.Items)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return ServiceResult<IEnumerable<OrderResponse>>.Success(orders.Select(MapToResponse));
    }

    // 6. GET TODOS (ADMIN) - PAGINADO
    public async Task<ServiceResult<PagedResponse<OrderResponse>>> GetAllOrdersAdminAsync(int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;

        var query = db.Orders.AsQueryable();

        // Contamos total real
        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        // Obtenemos página
        var orders = await query
            .Include(o => o.Items)
            .OrderByDescending(o => o.Id) // Importante: orden estable
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Mapeamos
        var mappedOrders = orders.Select(MapToResponse).ToList();

        var response = new PagedResponse<OrderResponse>(
            mappedOrders,
            totalCount,
            page,
            pageSize,
            totalPages
        );

        return ServiceResult<PagedResponse<OrderResponse>>.Success(response);
    }

    private static OrderResponse MapToResponse(Order order)
    {
        var items = order.Items.Select(i => new OrderItemDto(i.ProductId, i.ProductName, i.Quantity, i.UnitPrice)).ToList();

        return new OrderResponse(
            order.Id,
            order.UserId,
            order.CreatedAt,
            order.TotalAmount,
            order.Status.ToString(),
            items
        );
    }
}