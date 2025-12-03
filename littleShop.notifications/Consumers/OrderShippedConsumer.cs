using MassTransit;
using littleShop.Shared.Events;
using littleShop.notifications.Services;

namespace littleShop.notifications.Consumers;

public class OrderShippedConsumer(IEmailService emailService, ILogger<OrderShippedConsumer> logger)
    : IConsumer<OrderShippedEvent>
{
    public async Task Consume(ConsumeContext<OrderShippedEvent> context)
    {
        var msg = context.Message;
        logger.LogInformation("🚚 Pedido enviado #{Id}", msg.OrderId);

        var subject = $"¡Tu pedido #{msg.OrderId} está en camino! 🚚";

        var body = $@"
            <h3 style='color: #198754;'>¡Buenas noticias!</h3>
            <p>Tu pedido <strong>#{msg.OrderId}</strong> acaba de salir de nuestros almacenes.</p>
            <p>Número de seguimiento: <strong>{msg.TrackingNumber}</strong></p>
            <br>
            <p>¡Esperamos que lo disfrutes!</p>
        ";

        await emailService.SendEmailAsync(msg.Email, subject, body);
    }
}