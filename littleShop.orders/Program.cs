using littleshop.serviceDefaults;
using littleShop.orders.Data;
using littleShop.orders.DTOs;
using littleShop.orders.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// 1. DB
builder.AddNpgsqlDbContext<OrdersDbContext>("ordersdb");

// 2. Servicios
builder.Services.AddScoped<OrderService>();

// 3. Autenticación JWT (Necesario para saber el UserId)
// Copiamos la misma config que en Identity/Gateway
var jwtOptions = builder.Configuration.GetSection("Jwt");
var secretKey = jwtOptions["Key"]; // Asegúrate de tener esto en appsettings.json

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

// 4. OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();

app.MapDefaultEndpoints();

// 5. Migraciones
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
// ENDPOINTS (Protected)
// ==================================================================

var api = app.MapGroup("/api/v1/orders").WithTags("Orders").RequireAuthorization(); // <--- CANDADO 🔒

// GET /api/v1/orders (Mis pedidos)
api.MapGet("/", async (OrderService service, ClaimsPrincipal user) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId == null) return Results.Unauthorized();

    var result = await service.GetMyOrdersAsync(userId);
    return Results.Ok(result.Data);
});

// POST /api/v1/orders (Crear pedido)
api.MapPost("/", async (CreateOrderRequest request, OrderService service, ClaimsPrincipal user) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId == null) return Results.Unauthorized();

    var result = await service.CreateOrderAsync(userId, request);
    return Results.Created($"/api/v1/orders/{result.Data!.Id}", result.Data);
});

app.MapGet("/", () => Results.Redirect("/scalar/v1"));

app.Run();