namespace littleShop.orders.DTOs;

// Lo que recibimos del cliente
public record CreateOrderRequest(List<OrderItemDto> Items);
public record OrderItemDto(int ProductId, string ProductName, int Quantity, decimal UnitPrice);

// Lo que devolvemos
public record OrderResponse(int Id, string UserId, DateTime CreatedAt, decimal Total, List<OrderItemDto> Items);