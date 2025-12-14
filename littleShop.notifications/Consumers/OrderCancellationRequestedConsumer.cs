using MassTransit;
using littleShop.Shared.Events;
using littleShop.notifications.Services;

namespace littleShop.notifications.Consumers;

public class OrderCancellationRequestedConsumer(
    IEmailService emailService,
    IConfiguration configuration,
    ILogger<OrderCancellationRequestedConsumer> logger)
    : IConsumer<OrderCancellationRequestedEvent>
{
    public async Task Consume(ConsumeContext<OrderCancellationRequestedEvent> context)
    {
        var evt = context.Message;

        logger.LogInformation(
            "🚫 Solicitud de cancelación recibida - Pedido #{OrderId} por {Email}",
            evt.OrderId,
            evt.CustomerEmail
        );

        // Obtener email del admin desde configuración
        var adminEmail = configuration["AdminEmail"] ?? "admin@littleshop.com";

        var subject = $"🚫 Solicitud de Cancelación - Pedido #{evt.OrderId}";

        var body = $@"
            <h2>Solicitud de Cancelación de Pedido</h2>
            <p>Un cliente ha solicitado cancelar su pedido.</p>
            
            <div style='background-color: #fef3c7; border-left: 4px solid #f59e0b; padding: 15px; margin: 20px 0;'>
                <h3 style='margin-top: 0; color: #92400e;'>Detalles de la Solicitud:</h3>
                <ul style='list-style: none; padding: 0;'>
                    <li><strong>📦 Pedido ID:</strong> #{evt.OrderId}</li>
                    <li><strong>👤 Cliente:</strong> {evt.CustomerEmail}</li>
                    <li><strong>🆔 Usuario ID:</strong> {evt.UserId}</li>
                    <li><strong>📅 Fecha:</strong> {evt.RequestedAt:dd/MM/yyyy HH:mm}</li>
                    <li><strong>💬 Motivo:</strong> {evt.Reason}</li>
                </ul>
            </div>
            
            <div style='background-color: #dbeafe; border-left: 4px solid #3b82f6; padding: 15px; margin: 20px 0;'>
                <p style='margin: 0;'><strong>⚡ Acción requerida:</strong></p>
                <p style='margin: 10px 0 0 0;'>
                    Por favor, revisa este pedido en el panel de administración y procede con la 
                    cancelación si corresponde.
                </p>
            </div>
        ";

        try
        {
            await emailService.SendEmailAsync(adminEmail, subject, body);
            logger.LogInformation("✅ Email enviado a admin: {AdminEmail}", adminEmail);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Error al enviar email al admin");
        }
    }
}