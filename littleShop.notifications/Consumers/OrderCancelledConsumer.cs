using MassTransit;
using littleShop.Shared.Events;
using littleShop.notifications.Services;

namespace littleShop.notifications.Consumers;

public class OrderCancelledConsumer : IConsumer<OrderCancelledEvent>
{
    private readonly ILogger<OrderCancelledConsumer> _logger;
    private readonly IEmailService _emailService;

    public OrderCancelledConsumer(ILogger<OrderCancelledConsumer> logger, IEmailService emailService)
    {
        _logger = logger;
        _emailService = emailService;
    }

    public async Task Consume(ConsumeContext<OrderCancelledEvent> context)
    {
        var msg = context.Message;
        _logger.LogWarning("🚫 [PEDIDO CANCELADO] ID: {Id}", msg.OrderId);

        await _emailService.SendWelcomeEmailAsync(msg.Email,
            $"<h1 style='color:red'>Pedido #{msg.OrderId} Cancelado</h1><p>Motivo: {msg.Reason}</p>");
    }
}