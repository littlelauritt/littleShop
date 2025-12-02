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
    // Necesitamos el email para guardarlo en la BD
    var email = user.FindFirstValue(ClaimTypes.Email) ?? user.FindFirstValue("email");

    if (userId == null || email == null) return Results.Unauthorized();

    var result = await service.CreateOrderAsync(userId, email, request);

    if (!result.Succeeded)
        return Results.BadRequest(new { Error = result.Errors });

    return Results.Created($"/api/v1/orders/{result.Data!.Id}", result.Data);
});

// POST /{id}/cancel (Cancelar Pedido)
api.MapPost("/{id:int}/cancel", async (int id, OrderService service, ClaimsPrincipal user) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId == null) return Results.Unauthorized();

    // Ya no necesitamos pasar el email, el servicio lo lee de la BD
    var result = await service.CancelOrderAsync(id, userId);

    if (!result.Succeeded) return Results.BadRequest(result.Errors);

    return Results.Ok(new { Message = "Pedido cancelado correctamente" });
});

// GET /admin (Ver Todo - Solo Admin)
var adminApi = api.MapGroup("/admin");

adminApi.MapGet("/", async (OrderService service, ClaimsPrincipal user) =>
{
    if (!user.IsInRole("Admin")) return Results.Forbid();

    var result = await service.GetAllOrdersAdminAsync();
    return Results.Ok(result.Data);
});

// POST /admin/{id}/ship (Enviar pedido)
adminApi.MapPost("/{id:int}/ship", async (int id, OrderService service, ClaimsPrincipal user) =>
{
    // Verificamos rol de admin
    if (!user.IsInRole("Admin")) return Results.Forbid();

    var result = await service.ShipOrderAsync(id);

    if (!result.Succeeded)
        return Results.BadRequest(new { Error = result.Errors });

    return Results.Ok(new { Message = "Pedido marcado como enviado" });
});

app.MapGet("/", () => Results.Redirect("/scalar/v1"));

app.Run();