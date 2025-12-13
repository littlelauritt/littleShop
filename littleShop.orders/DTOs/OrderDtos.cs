using System.ComponentModel.DataAnnotations;

namespace littleShop.orders.DTOs;

// ✅ Request para crear pedido (SIN ProductName)
public record CreateOrderRequest(
    List<OrderItemDto> Items,
    string ShippingAddress
);

// ✅ DTO para items en la request (sin ProductName)
public record OrderItemDto(
    int ProductId,
    int Quantity,
    decimal UnitPrice
);

// ✅ Response de pedido
public record OrderResponse(
    int Id,
    string UserId,
    string CustomerEmail,
    DateTime CreatedAt,
    decimal TotalAmount,
    string Status,
    string ShippingAddress,
    List<OrderItemResponseDto> Items
);

// ✅ DTO para items en la response (CON ProductName)
public record OrderItemResponseDto(
    int ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice
);

// ✅ Respuesta paginada
public record PagedResponse<T>(
    IEnumerable<T> Items,
    int TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages
);