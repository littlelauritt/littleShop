using MassTransit;
using littleShop.Shared.Events;
using littleShop.notifications.Services;

namespace littleShop.notifications.Consumers;

public class OrderCancelledConsumer(IEmailService emailService, ILogger<OrderCancelledConsumer> logger)
    : IConsumer<OrderCancelledEvent>
{
    public async Task Consume(ConsumeContext<OrderCancelledEvent> context)
    {
        var msg = context.Message;
        logger.LogInformation("❌ Pedido cancelado #{Id}. Motivo: {Reason}", msg.OrderId, msg.Reason);

        var subject = $"Pedido #{msg.OrderId} Cancelado";

        var body = $@"
            <h3 style='color: #dc3545;'>Tu pedido ha sido cancelado</h3>
            <p>Lamentamos informarte que el pedido <strong>#{msg.OrderId}</strong> ha sido cancelado.</p>
            <p><strong>Motivo:</strong> {msg.Reason}</p>
            <br>
            <p>Si ya habías pagado, recibirás el reembolso en los próximos días.</p>
        ";

        await emailService.SendEmailAsync(msg.Email, subject, body);
    }
}