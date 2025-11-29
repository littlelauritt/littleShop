namespace littleShop.Shared.Events;

public record UserCreatedEvent(string UserId, string Email, DateTime CreatedAt);