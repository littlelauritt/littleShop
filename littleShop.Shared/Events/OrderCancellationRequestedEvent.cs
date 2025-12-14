namespace littleShop.Shared.Events;

// Evento cuando un usuario solicita cancelación
public record OrderCancellationRequestedEvent(
    int OrderId,
    string UserId,
    string CustomerEmail,
    string Reason,
    DateTime RequestedAt
);