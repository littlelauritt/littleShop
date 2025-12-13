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
// IMPORTANTE: En Docker, esto buscará el servicio por nombre de contenedor
builder.Services.AddHttpClient("catalog-api", client =>
{
    client.BaseAddress = new Uri("http://catalog-api:8080");
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
// Sacamos esto fuera del 'if Development'
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
    options.WithTheme(ScalarTheme.Moon); // Tema oscuro para diferenciar
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

adminApi.MapGet("/", async (int? page, int? pageSize, OrderService service, ClaimsPrincipal user) =>
{
    if (!user.IsInRole("Admin")) return Results.Forbid();
    var result = await service.GetAllOrdersAdminAsync(page ?? 1, pageSize ?? 10);
    return Results.Ok(result.Data);
});

app.MapGet("/", () => Results.Redirect("/scalar/v1"));

app.Run();