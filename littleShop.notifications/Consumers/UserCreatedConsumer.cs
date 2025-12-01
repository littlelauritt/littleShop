using MassTransit;
using littleShop.Shared.Events;
using littleShop.notifications.Services;

namespace littleShop.notifications.Consumers;

public class UserCreatedConsumer : IConsumer<UserCreatedEvent>
{
    private readonly ILogger<UserCreatedConsumer> _logger;
    private readonly IEmailService _emailService; // 1. Nueva variable para el servicio

    // 2. Inyectamos el servicio de email en el constructor
    public UserCreatedConsumer(ILogger<UserCreatedConsumer> logger, IEmailService emailService)
    {
        _logger = logger;
        _emailService = emailService;
    }

    public async Task Consume(ConsumeContext<UserCreatedEvent> context)
    {
        var message = context.Message;

        _logger.LogInformation("📨 [WORKER] Mensaje recibido desde RabbitMQ para: {Email}", message.Email);

        try
        {
            // 3. Llamamos al servicio real para enviar el correo
            await _emailService.SendWelcomeEmailAsync(message.Email, message.UserId);

            _logger.LogInformation("✅ Email enviado correctamente a MailDev.");
        }
        catch (Exception ex)
        {
            // Si falla el envío (ej. MailDev no está listo), logueamos el error
            _logger.LogError(ex, "❌ Error enviando el email de bienvenida.");

            // Nota: Si quisieras que RabbitMQ reintente el mensaje más tarde, 
            // deberías lanzar la excepción aquí (throw;) en vez de solo loguearla.
        }
    }
}