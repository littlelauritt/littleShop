using littleshop.serviceDefaults;
using littleShop.notifications.Consumers;
using littleShop.notifications.Services;
using MassTransit;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddTransient<IEmailService, EmailService>();

builder.Services.AddMassTransit(x =>
{
    // 1. Registro Usuario
    x.AddConsumer<UserCreatedConsumer>();

    // 2. Nuevo Pedido
    x.AddConsumer<OrderCreatedConsumer>();

    // 3. Cancelar Pedido
    x.AddConsumer<OrderCancelledConsumer>();

    // 4. ENVÍO DE PEDIDO 

    x.AddConsumer<OrderShippedConsumer>();

    // 5. Solicitud de Cancelación
    x.AddConsumer<OrderCancellationRequestedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var config = context.GetRequiredService<IConfiguration>();
        var conn = config.GetConnectionString("messaging");
        if (!string.IsNullOrEmpty(conn)) cfg.Host(new Uri(conn));

        cfg.UseMessageRetry(r => r.Intervals(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5)));
        cfg.ConfigureEndpoints(context);
    });
});

var host = builder.Build();
host.Run();