using littleshop.serviceDefaults;
using littleShop.notifications.Consumers; // Tu carpeta de consumidores
using MassTransit;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddMassTransit(x =>
{
    // 1. Registramos tu consumidor
    x.AddConsumer<UserCreatedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        // 2. LÓGICA DEL PROFESOR (Adaptada a ti)
        var configuration = context.GetRequiredService<IConfiguration>();

        // ¡IMPORTANTE! Usamos "messaging" porque así lo llamaste en el AppHost
        var connectionString = configuration.GetConnectionString("messaging");

        if (!string.IsNullOrEmpty(connectionString))
        {
            // Esto coge la URL completa (amqp://guest:guest@messaging:5672...)
            cfg.Host(new Uri(connectionString));
        }

        // 3. Política de reintentos (Muy útil si Rabbit tarda en arrancar)
        cfg.UseMessageRetry(r => r.Intervals(
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(15)));

        // 4. Configura las colas automáticamente
        cfg.ConfigureEndpoints(context);
    });
});

var host = builder.Build();
host.Run();