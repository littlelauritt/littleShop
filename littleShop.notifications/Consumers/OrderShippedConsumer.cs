using MassTransit;
using littleShop.Shared.Events;
using littleShop.notifications.Services;

namespace littleShop.notifications.Consumers;

public class OrderShippedConsumer : IConsumer<OrderShippedEvent>
{
    private readonly ILogger<OrderShippedConsumer> _logger;
    private readonly IEmailService _emailService;

    public OrderShippedConsumer(ILogger<OrderShippedConsumer> logger, IEmailService emailService)
    {
        _logger = logger;
        _emailService = emailService;
    }

    public async Task Consume(ConsumeContext<OrderShippedEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation("🚚 [WORKER] Pedido #{Id} ENVIADO. Tracking: {Tracking}", msg.OrderId, msg.TrackingNumber);

        await _emailService.SendWelcomeEmailAsync(msg.Email,
            $"<h1>¡Tu pedido ha salido! 🚚</h1><p>El pedido #{msg.OrderId} está en camino.</p><p>Número de seguimiento: <b>{msg.TrackingNumber}</b></p>");
    }
}