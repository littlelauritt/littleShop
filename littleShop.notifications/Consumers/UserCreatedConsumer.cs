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
        logger.LogInformation("👤 Usuario registrado: {Email}. Enviando confirmación...", msg.Email);

        var frontendUrl = "http://localhost:5173";
        var verificationLink = $"{frontendUrl}/verify-email?userId={msg.UserId}&code={msg.ConfirmationToken}";

        var subject = "Verifica tu cuenta en LittleShop ✅";

        var body = $@"
            <h2>¡Bienvenido a LittleShop!</h2>
            <p>Gracias por registrarte. Para empezar a comprar, necesitas confirmar tu dirección de correo electrónico.</p>
            <div style='text-align: center; margin: 30px 0;'>
                <a href='{verificationLink}' style='background-color: #10B981; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; font-weight: bold; font-size: 16px;'>
                    Confirmar mi Email
                </a>
            </div>
            <p style='font-size: 0.9em;'>Si el botón no funciona, copia y pega este enlace en tu navegador:</p>
            <p style='font-size: 0.8em; color: #666;'>{verificationLink}</p>
        ";

        await emailService.SendEmailAsync(msg.Email, subject, body);
    }
}