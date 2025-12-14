using littleShop.orders.Data;
using littleShop.orders.DTOs;
using littleShop.orders.Entities;
using littleShop.orders.Shared;
using littleShop.Shared;
using littleShop.Shared.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace littleShop.orders.Services;

public class OrderService
{
    private readonly OrdersDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IPublishEndpoint _publishEndpoint;

    public OrderService(
        OrdersDbContext context,
        IHttpClientFactory httpClientFactory,
        IPublishEndpoint publishEndpoint)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<ServiceResult<OrderResponse>> CreateOrderAsync(
        string userId,
        string email,
        CreateOrderRequest request)
    {
        try
        {
            if (request.Items == null || !request.Items.Any())
                return ServiceResult<OrderResponse>.Failure("El pedido debe tener al menos un producto.");

            if (string.IsNullOrWhiteSpace(request.ShippingAddress))
                return ServiceResult<OrderResponse>.Failure("La dirección de envío es obligatoria.");

            var catalogClient = _httpClientFactory.CreateClient("catalog-api");
            var orderItems = new List<OrderItem>();

            foreach (var item in request.Items)
            {
                var productResponse = await catalogClient.GetAsync($"/api/v1/products/{item.ProductId}");

                if (!productResponse.IsSuccessStatusCode)
                    return ServiceResult<OrderResponse>.Failure($"Producto {item.ProductId} no encontrado.");

                var productJson = await productResponse.Content.ReadAsStringAsync();
                var product = JsonSerializer.Deserialize<ProductDto>(productJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                orderItems.Add(new OrderItem
                {
                    ProductId = item.ProductId,
                    ProductName = product?.Name ?? "Producto desconocido",
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                });
            }

            var order = new Order
            {
                UserId = userId,
                CustomerEmail = email,
                ShippingAddress = request.ShippingAddress,
                TotalAmount = orderItems.Sum(i => i.Quantity * i.UnitPrice),
                Status = OrderStatus.Pending,
                Items = orderItems
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            foreach (var item in request.Items)
            {
                var stockRequest = new { Stock = item.Quantity };
                var content = new StringContent(
                    JsonSerializer.Serialize(stockRequest),
                    Encoding.UTF8,
                    "application/json"
                );

                await catalogClient.PostAsync(
                    $"/api/v1/products/{item.ProductId}/reduce-stock",
                    content
                );
            }

            await _publishEndpoint.Publish(new OrderCreatedEvent(
                order.Id,
                userId,
                email,
                order.TotalAmount,
                order.CreatedAt
            ));

            var response = new OrderResponse(
                order.Id,
                order.UserId,
                order.CustomerEmail,
                order.CreatedAt,
                order.TotalAmount,
                order.Status.ToString(),
                order.ShippingAddress,
                order.Items.Select(i => new OrderItemResponseDto(
                    i.ProductId,
                    i.ProductName,
                    i.Quantity,
                    i.UnitPrice
                )).ToList()
            );

            return ServiceResult<OrderResponse>.Success(response);
        }
        catch (Exception ex)
        {
            return ServiceResult<OrderResponse>.Failure($"Error creando pedido: {ex.Message}");
        }
    }

    public async Task<ServiceResult<List<OrderResponse>>> GetMyOrdersAsync(string userId)
    {
        var orders = await _context.Orders
            .Include(o => o.Items)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        var response = orders.Select(o => new OrderResponse(
    o.Id,
    o.UserId,
    o.CustomerEmail,
    o.CreatedAt,
    o.TotalAmount,
    o.Status.ToString(),
    o.ShippingAddress,
    o.Items.Select(i => new OrderItemResponseDto(
        i.ProductId,
        i.ProductName,
        i.Quantity,
        i.UnitPrice
    )).ToList(),
    o.CancellationRequested,           
    o.CancellationRequestedAt,         
    o.CancellationReason            
)).ToList();

        return ServiceResult<List<OrderResponse>>.Success(response);
    }

    public async Task<ServiceResult<PagedResponse<OrderResponse>>> GetAllOrdersAdminAsync(int page, int pageSize)
    {
        var totalCount = await _context.Orders.CountAsync();

        var orders = await _context.Orders
            .Include(o => o.Items)
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = orders.Select(o => new OrderResponse(
            o.Id,
            o.UserId,
            o.CustomerEmail,
            o.CreatedAt,
            o.TotalAmount,
            o.Status.ToString(),
            o.ShippingAddress,
            o.Items.Select(i => new OrderItemResponseDto(
                i.ProductId,
                i.ProductName,
                i.Quantity,
                i.UnitPrice
            )).ToList(),
            o.CancellationRequested,
            o.CancellationRequestedAt,
            o.CancellationReason
        )).ToList();

        var response = new PagedResponse<OrderResponse>(
            items,
            totalCount,
            page,
            pageSize,
            (int)Math.Ceiling(totalCount / (double)pageSize)
        );

        return ServiceResult<PagedResponse<OrderResponse>>.Success(response);
    }

    public async Task<ServiceResult<bool>> RequestCancellationAsync(int orderId, string userId, string reason)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

        if (order == null)
            return ServiceResult<bool>.Failure("Pedido no encontrado.");

        if (order.Status == OrderStatus.Cancelled)
            return ServiceResult<bool>.Failure("El pedido ya está cancelado.");

        if (order.Status != OrderStatus.Pending)
            return ServiceResult<bool>.Failure("Solo se pueden solicitar cancelaciones de pedidos pendientes.");

        if (order.CancellationRequested)
            return ServiceResult<bool>.Failure("Ya existe una solicitud de cancelación para este pedido.");

        order.CancellationRequested = true;
        order.CancellationRequestedAt = DateTime.UtcNow;
        order.CancellationReason = reason;
        await _context.SaveChangesAsync();

        await _publishEndpoint.Publish(new OrderCancellationRequestedEvent(
            order.Id,
            order.UserId,
            order.CustomerEmail,
            reason,
            DateTime.UtcNow
        ));

        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<bool>> CancelOrderAdminAsync(int orderId)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
            return ServiceResult<bool>.Failure("Pedido no encontrado.");

        if (order.Status == OrderStatus.Cancelled)
            return ServiceResult<bool>.Failure("El pedido ya está cancelado.");

        if (order.Status != OrderStatus.Pending)
            return ServiceResult<bool>.Failure("Solo se pueden cancelar pedidos pendientes.");

        order.Status = OrderStatus.Cancelled;
        order.CancellationRequested = false;
        await _context.SaveChangesAsync();

        var itemsToRestore = order.Items.ToDictionary(
            i => i.ProductId,
            i => i.Quantity
        );

        await _publishEndpoint.Publish(new OrderCancelledEvent(
            order.Id,
            order.CustomerEmail,
            order.CancellationReason ?? "Cancelado por administrador",
            itemsToRestore
        ));

        return ServiceResult<bool>.Success(true);
    }

    public async Task<bool> ShipOrderAsync(int orderId)
    {
        var order = await _context.Orders.FindAsync(orderId);

        if (order == null ||
           (order.Status != OrderStatus.Confirmed && order.Status != OrderStatus.Pending))
        {
            return false;
        }

        order.Status = OrderStatus.Shipped;
        await _context.SaveChangesAsync();

        var trackingNumber = "TRK-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper();

        await _publishEndpoint.Publish(new OrderShippedEvent(
            order.Id,
            order.CustomerEmail,
            trackingNumber 
        ));

        return true;
    }
}

internal record ProductDto(int Id, string Name, decimal Price);