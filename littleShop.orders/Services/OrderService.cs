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
        // A. Validar Stock (Llamada HTTP a Catalog)
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

        // B. Guardar en Base de Datos
        var order = new Order
        {
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            Status = OrderStatus.Confirmed,
            CustomerEmail = userEmail, // <--- GUARDAMOS EMAIL REAL
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

        // C. Publicar Evento (Fire & Forget)
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
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error publicando evento de pedido: {ex.Message}");
            }
        });

        return ServiceResult<OrderResponse>.Success(MapToResponse(order));
    }

    // 2. ENVIAR PEDIDO (Admin)
    public async Task<ServiceResult> ShipOrderAsync(int orderId)
    {
        var order = await db.Orders.FindAsync(orderId);
        if (order is null) return ServiceResult.Failure("Pedido no encontrado");
        if (order.Status != OrderStatus.Confirmed) return ServiceResult.Failure("Solo se envían pedidos confirmados");

        order.Status = OrderStatus.Shipped;
        await db.SaveChangesAsync();

        // Evento de Envío (Fire & Forget)
        // (Opcional: si quieres implementar OrderShippedEvent)
        return ServiceResult.Success();
    }

    // 3. CANCELAR PEDIDO
    public async Task<ServiceResult> CancelOrderAsync(int orderId, string userId)
    {
        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == orderId);

        if (order is null) return ServiceResult.Failure("Pedido no encontrado");
        if (order.UserId != userId) return ServiceResult.Failure("No tienes permiso para cancelar este pedido");
        if (order.Status == OrderStatus.Cancelled) return ServiceResult.Failure("El pedido ya estaba cancelado");

        order.Status = OrderStatus.Cancelled;
        await db.SaveChangesAsync();

        // Evento de cancelación
        _ = Task.Run(async () =>
        {
            try
            {
                // Usamos order.CustomerEmail que guardamos al crear el pedido
                await publishEndpoint.Publish(new OrderCancelledEvent(
                    order.Id,
                    order.CustomerEmail,
                    "Cancelado por el usuario"
                ));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error publicando evento de cancelación: {ex.Message}");
            }
        });

        return ServiceResult.Success();
    }

    // 4. GET MIS PEDIDOS
    public async Task<ServiceResult<IEnumerable<OrderResponse>>> GetMyOrdersAsync(string userId)
    {
        var orders = await db.Orders
            .Include(o => o.Items)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return ServiceResult<IEnumerable<OrderResponse>>.Success(orders.Select(MapToResponse));
    }

    // 5. GET TODOS (ADMIN)
    public async Task<ServiceResult<IEnumerable<OrderResponse>>> GetAllOrdersAdminAsync()
    {
        var orders = await db.Orders
            .Include(o => o.Items)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return ServiceResult<IEnumerable<OrderResponse>>.Success(orders.Select(MapToResponse));
    }

    private static OrderResponse MapToResponse(Order order)
    {
        var items = order.Items.Select(i => new OrderItemDto(i.ProductId, i.ProductName, i.Quantity, i.UnitPrice)).ToList();

        // Añado order.Status.ToString() para que salga "Pending", "Confirmed", etc.
        return new OrderResponse(
            order.Id,
            order.UserId,
            order.CreatedAt,
            order.TotalAmount,
            order.Status.ToString(), // <--- AÑADE ESTO
            items
        );
    }
}