using littleshop.serviceDefaults;
using littleShop.orders.Data;
using littleShop.orders.DTOs;
using littleShop.orders.Services;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// 1. Base de Datos
builder.AddNpgsqlDbContext<OrdersDbContext>("ordersdb");

// 2. Servicios
builder.Services.AddScoped<OrderService>();

// 3. Cliente HTTP (HÍBRIDO: Detector de Entorno Infalible)
builder.Services.AddHttpClient("catalog-api", client =>
{
    // Esta variable la pone .NET automáticamente cuando está dentro de un Docker real.
    // En tu ordenador (Aspire), esto será falso o nulo.
    var esDocker = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";

    if (esDocker)
    {
        // --- ESTAMOS EN GITHUB ACTIONS / DOCKER COMPOSE ---
        // Usamos la dirección fija interna de la red Docker.
        client.BaseAddress = new Uri("http://catalog-api:8080");
        Console.WriteLine("🐳 Orders detectó MODO DOCKER -> Conectando a catalog-api:8080");
    }
    else
    {
        // --- ESTAMOS EN ASPIRE (TU ORDENADOR) ---
        // Usamos el nombre del recurso definido en AppHost: "littleshop-catalog".
        // La magia de 'https+http' deja que Aspire elija el puerto dinámico.
        client.BaseAddress = new Uri("https+http://littleshop-catalog");
        Console.WriteLine("💜 Orders detectó MODO ASPIRE -> Conectando a littleshop-catalog (Service Discovery)");
    }
});

// 4. RabbitMQ (MassTransit)
builder.Services.AddMassTransit(bus =>
{
    bus.SetKebabCaseEndpointNameFormatter();
    bus.UsingRabbitMq((context, cfg) =>
    {
        var configuration = context.GetRequiredService<IConfiguration>();
        var connectionString = configuration.GetConnectionString("messaging");

        if (!string.IsNullOrEmpty(connectionString))
            cfg.Host(new Uri(connectionString));
        else
            cfg.Host("messaging", "/");
    });
});

// 5. Autenticación JWT
var jwtOptions = builder.Configuration.GetSection("Jwt");
var secretKey = jwtOptions["Key"];

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false; // Importante para Docker interno
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions["Issuer"],
            ValidAudience = jwtOptions["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!))
        };
    });
builder.Services.AddAuthorization();

// 6. OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();

app.MapDefaultEndpoints();

// 7. MIGRACIONES Y DOCS (Corregido para Docker)
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        await db.Database.MigrateAsync();
        Console.WriteLine("✅ Base de datos de Orders migrada.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ Error migrando DB Orders: {ex.Message}");
    }
}

app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.WithTitle("LittleShop Orders API");
    options.WithTheme(ScalarTheme.Moon);
});

app.UseAuthentication();
app.UseAuthorization();

// ==================================================================
// ENDPOINTS
// ==================================================================

var api = app.MapGroup("/api/v1/orders").WithTags("Orders").RequireAuthorization();

api.MapGet("/", async (OrderService service, ClaimsPrincipal user) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId == null) return Results.Unauthorized();
    var result = await service.GetMyOrdersAsync(userId);
    return Results.Ok(result.Data);
});

api.MapPost("/", async (CreateOrderRequest request, OrderService service, ClaimsPrincipal user) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    var email = user.FindFirstValue(ClaimTypes.Email) ?? user.FindFirstValue("email");

    if (userId == null) return Results.Unauthorized();
    var safeEmail = email ?? "unknown@littleshop.local";

    var result = await service.CreateOrderAsync(userId, safeEmail, request);

    if (!result.Succeeded)
        return Results.BadRequest(new { Error = result.Errors });

    return Results.Created($"/api/v1/orders/{result.Data!.Id}", result.Data);
});

// Admin endpoints
var adminApi = api.MapGroup("/admin");

// 1. Listar todos (YA LO TENÍAS)
adminApi.MapGet("/", async (int? page, int? pageSize, OrderService service, ClaimsPrincipal user) =>
{
    if (!user.IsInRole("Admin")) return Results.Forbid();
    var result = await service.GetAllOrdersAdminAsync(page ?? 1, pageSize ?? 10);
    return Results.Ok(result.Data);
});

// 2. ENVIAR PEDIDO (ESTE FALTABA) 🚚
adminApi.MapPost("/{id}/ship", async (int id, OrderService service, ClaimsPrincipal user) =>
{
    if (!user.IsInRole("Admin")) return Results.Forbid();

    var success = await service.ShipOrderAsync(id);

    if (!success) return Results.BadRequest("No se pudo enviar el pedido (quizás no existe o ya está enviado).");
    return Results.NoContent();
});

// 3. CANCELAR PEDIDO ADMIN (ESTE FALTABA) ❌
adminApi.MapPost("/{id}/cancel", async (int id, OrderService service, ClaimsPrincipal user) =>
{
    if (!user.IsInRole("Admin")) return Results.Forbid();

    var success = await service.CancelOrderAdminAsync(id);

    if (!success) return Results.BadRequest("No se pudo cancelar el pedido.");
    return Results.NoContent();
});

app.MapGet("/", () => Results.Redirect("/scalar/v1"));

app.Run();