using MassTransit;
using littleShop.Shared.Events;
using littleShop.notifications.Services;

namespace littleShop.notifications.Consumers;

public class UserCreatedConsumer(IEmailService emailService, ILogger<UserCreatedConsumer> logger)
    : IConsumer<UserCreatedEvent>
{
    public async Task Consume(ConsumeContext<UserCreatedEvent> context)
    {
        var msg = context.Message;
        logger.LogInformation("👤 Usuario registrado: {Email}", msg.Email);

        // Usamos el método específico de bienvenida
        await emailService.SendWelcomeEmailAsync(msg.Email, msg.UserId);
    }
}