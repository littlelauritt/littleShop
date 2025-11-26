namespace littleShop.identity.Events;

public record UserCreatedEvent(string UserId, string Email, DateTime CreatedAt);