namespace littleShop.Shared.Events;

public record OrderCreatedEvent(
    int OrderId,
    string UserId,
    string Email,
    decimal TotalAmount,
    DateTime CreatedAt
);