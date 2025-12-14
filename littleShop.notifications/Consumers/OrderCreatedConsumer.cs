using MassTransit;
using littleShop.Shared.Events;
using littleShop.notifications.Services;

namespace littleShop.notifications.Consumers;

public class OrderCreatedConsumer(IEmailService emailService, ILogger<OrderCreatedConsumer> logger)
    : IConsumer<OrderCreatedEvent>
{
    public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
    {
        var msg = context.Message;
        logger.LogInformation("🛒 Pedido creado #{Id} para {Email}", msg.OrderId, msg.Email);

        var subject = $"Confirmación de Pedido #{msg.OrderId} 🛍️";

        var body = $@"
            <h3>¡Gracias por tu compra!</h3>
            <p>Hemos recibido tu pedido correctamente.</p>
            <p><strong>Importe Total:</strong> {msg.TotalAmount:C} </p>
            <p>Fecha: {msg.CreatedAt.ToLocalTime()}</p>
            <br>
            <p>Te avisaremos cuando salga de nuestro almacén.</p>
        ";

        await emailService.SendEmailAsync(msg.Email, subject, body);
    }
}