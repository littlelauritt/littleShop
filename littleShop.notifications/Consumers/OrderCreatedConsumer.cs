using MassTransit;
using littleShop.Shared.Events;
using littleShop.notifications.Services;

namespace littleShop.notifications.Consumers;

public class OrderCreatedConsumer : IConsumer<OrderCreatedEvent>
{
    private readonly ILogger<OrderCreatedConsumer> _logger;
    private readonly IEmailService _emailService;

    public OrderCreatedConsumer(ILogger<OrderCreatedConsumer> logger, IEmailService emailService)
    {
        _logger = logger;
        _emailService = emailService;
    }

    public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation("📦 [WORKER] Nuevo pedido #{Id} de {Email}. Total: {Total}€", msg.OrderId, msg.Email, msg.TotalAmount);

        try
        {
            await _emailService.SendWelcomeEmailAsync(msg.Email,
                $"<h1>¡Gracias por tu compra!</h1><p>Tu pedido #{msg.OrderId} por valor de <b>{msg.TotalAmount}€</b> ha sido confirmado correctamente.</p>");

            _logger.LogInformation("✅ Email de pedido enviado.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error enviando email de pedido.");
        }
    }
}