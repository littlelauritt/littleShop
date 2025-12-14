namespace littleShop.Shared.Events;

public record OrderCancellationRequestedEvent(
    int OrderId,
    string UserId,
    string CustomerEmail,
    string Reason,
    DateTime RequestedAt
);