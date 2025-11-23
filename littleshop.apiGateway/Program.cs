using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using littleshop.serviceDefaults;

var builder = WebApplication.CreateBuilder(args);

// Esto arregla los errores CS1061 de ServiceDefaults
builder.AddServiceDefaults();

// --- Configuración de YARP ---
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver(); // <--- ESTO ARREGLA EL ERROR 3 (gracias al paquete del PASO A)

var app = builder.Build();

// Mapear endpoints de salud y métricas
app.MapDefaultEndpoints();

// Activar el Gateway
app.MapReverseProxy();

app.MapGet("/", () =>
{
    return Results.Ok(new
    {
        Status = "Healthy",
        Service = "LittleShop API Gateway",
        Message = "Bienvenido a la puerta de enlace 👋"
    });
});

app.Run();