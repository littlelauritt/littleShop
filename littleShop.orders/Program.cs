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

// 3. Cliente HTTP (Para llamar al Catálogo)
builder.Services.AddHttpClient("catalog-api", client =>
{
    client.BaseAddress = new Uri("https+http://littleshop-catalog");
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

// 7. Migraciones Automáticas
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
    await db.Database.MigrateAsync();
}

app.UseAuthentication();
app.UseAuthorization();

// ==================================================================
// ENDPOINTS
// ==================================================================

var api = app.MapGroup("/api/v1/orders").WithTags("Orders").RequireAuthorization();

// GET / (Ver mis pedidos)
api.MapGet("/", async (OrderService service, ClaimsPrincipal user) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId == null) return Results.Unauthorized();

    var result = await service.GetMyOrdersAsync(userId);
    return Results.Ok(result.Data);
});

// POST / (Crear pedido)
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

// POST /{id}/cancel (Cancelar Pedido - Usuario)
api.MapPost("/{id:int}/cancel", async (int id, OrderService service, ClaimsPrincipal user) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId == null) return Results.Unauthorized();

    var result = await service.CancelOrderAsync(id, userId);

    if (!result.Succeeded) return Results.BadRequest(result.Errors);

    return Results.Ok(new { Message = "Pedido cancelado correctamente" });
});

// --- ADMIN ENDPOINTS ---
// Usamos MapGroup para agrupar rutas de admin
var adminApi = api.MapGroup("/admin");

// GET /api/v1/orders/admin (Ver Todo Paginado) - CORREGIDO
adminApi.MapGet("/", async (int? page, int? pageSize, OrderService service, ClaimsPrincipal user) =>
{
    if (!user.IsInRole("Admin")) return Results.Forbid();

    // AQUÍ ESTABA EL ERROR: Faltaba pasar los parámetros page y pageSize
    var result = await service.GetAllOrdersAdminAsync(page ?? 1, pageSize ?? 10);
    return Results.Ok(result.Data);
});

// POST /api/v1/orders/admin/{id}/ship (Enviar pedido)
adminApi.MapPost("/{id:int}/ship", async (int id, OrderService service, ClaimsPrincipal user) =>
{
    if (!user.IsInRole("Admin")) return Results.Forbid();

    var result = await service.ShipOrderAsync(id);

    if (!result.Succeeded)
        return Results.BadRequest(new { Error = result.Errors });

    return Results.Ok(new { Message = "Pedido marcado como enviado" });
});

// POST /api/v1/orders/admin/{id}/cancel (Cancelar Admin)
adminApi.MapPost("/{id:int}/cancel", async (int id, OrderService service, ClaimsPrincipal user) =>
{
    if (!user.IsInRole("Admin")) return Results.Forbid();

    var result = await service.CancelOrderAdminAsync(id);

    if (!result.Succeeded)
        return Results.BadRequest(new { Error = result.Errors });

    return Results.Ok(new { Message = "Pedido cancelado por admin" });
});

app.MapGet("/", () => Results.Redirect("/scalar/v1"));

app.Run();