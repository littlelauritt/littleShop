using System.ComponentModel.DataAnnotations;

namespace littleShop.orders.DTOs;

public record CreateOrderRequest(List<OrderItemDto> Items);
public record OrderItemDto(int ProductId, string ProductName, int Quantity, decimal UnitPrice);

public record OrderResponse(
    int Id,
    string UserId,
    DateTime CreatedAt,
    decimal Total,
    string Status,
    List<OrderItemDto> Items
);

// --- NUEVO: RESPUESTA PAGINADA ---
public record PagedResponse<T>(
    IEnumerable<T> Items,
    int TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages
);