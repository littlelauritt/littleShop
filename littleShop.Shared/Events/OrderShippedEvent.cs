namespace littleShop.Shared.Events;

public record OrderShippedEvent(
    int OrderId,
    string Email,
    string TrackingNumber
);