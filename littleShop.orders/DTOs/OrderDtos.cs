using System.ComponentModel.DataAnnotations;

namespace littleShop.orders.DTOs;

public record CreateOrderRequest(
    List<OrderItemDto> Items,
    string ShippingAddress
);

public record OrderItemDto(
    int ProductId,
    int Quantity,
    decimal UnitPrice
);

public record OrderResponse(
    int Id,
    string UserId,
    string CustomerEmail,
    DateTime CreatedAt,
    decimal Total,
    string Status,
    string ShippingAddress,
    List<OrderItemResponseDto> Items,
    bool CancellationRequested = false,           
    DateTime? CancellationRequestedAt = null,     
    string? CancellationReason = null             
);

public record OrderItemResponseDto(
    int ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice
);

public record PagedResponse<T>(
    IEnumerable<T> Items,
    int TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages
);

public record RequestCancellationDto(string Reason);