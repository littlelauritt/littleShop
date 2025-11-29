using MassTransit;
using littleShop.Shared.Events;

namespace littleShop.notifications.Consumers;

public class UserCreatedConsumer : IConsumer<UserCreatedEvent>
{
    private readonly ILogger<UserCreatedConsumer> _logger;

    public UserCreatedConsumer(ILogger<UserCreatedConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<UserCreatedEvent> context)
    {
        var message = context.Message;

        _logger.LogInformation("📨 [WORKER] Mensaje recibido desde RabbitMQ:");
        _logger.LogInformation("   - Usuario ID: {Id}", message.UserId);
        _logger.LogInformation("   - Email: {Email}", message.Email);
        _logger.LogInformation("   - Fecha: {Date}", message.CreatedAt);

        _logger.LogInformation("✅ Email de bienvenida enviado (Simulado).");

        return Task.CompletedTask;
    }
}