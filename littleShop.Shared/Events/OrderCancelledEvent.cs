namespace littleShop.Shared.Events;

public record OrderCancelledEvent(
    int OrderId,
    string Email,
    string Reason
);