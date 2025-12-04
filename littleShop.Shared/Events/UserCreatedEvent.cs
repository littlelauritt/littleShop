namespace littleShop.Shared.Events;

public record UserCreatedEvent(
    string UserId,
    string Email,
    string ConfirmationToken,
    DateTime CreatedAt
);