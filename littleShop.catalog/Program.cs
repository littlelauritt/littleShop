using littleshop.serviceDefaults;
using littleShop.catalog.Data;
using littleShop.catalog.DTOs;
using littleShop.catalog.Services;
using littleShop.catalog.Consumers;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// 1. CONEXIÓN A BASE DE DATOS (Postgres)
builder.AddNpgsqlDbContext<CatalogDbContext>("catalogdb");

// 2. SERVICIOS
builder.Services.AddScoped<ProductService>();

// 3. CONFIGURACIÓN MASSTRANSIT
builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();

    // Registramos el consumidor que creamos
    x.AddConsumer<OrderCancelledConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var configuration = context.GetRequiredService<IConfiguration>();
        var connectionString = configuration.GetConnectionString("messaging");

        Console.WriteLine($"🔍 [DEBUG RABBIT] ConnectionString recibida: '{connectionString ?? "NULA"}'");

        if (!string.IsNullOrEmpty(connectionString)) cfg.Host(new Uri(connectionString));
        else cfg.Host("messaging", "/");

        cfg.ConfigureEndpoints(context);
    });
});

// 4. OPENAPI (SCALAR)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// =========================================================
// AQUÍ SE CONSTRUYE LA APP
// =========================================================
var app = builder.Build();

app.MapDefaultEndpoints();

// 5. MIGRACIONES AUTOMÁTICAS Y MIDDLEWARES
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    await db.Database.MigrateAsync();
}

// =========================================================
// DEFINICIÓN DE ENDPOINTS
// =========================================================

var api = app.MapGroup("/api/v1/products").WithTags("Products");

// GET /api/v1/products (AHORA CON PAGINACIÓN)
// Recibimos 'page' y 'pageSize' como Query Params opcionales (defaults en servicio: 1 y 10)
api.MapGet("/", async (int? page, int? pageSize, ProductService service) =>
{
    // Pasamos los valores (o null, el servicio pone los defaults)
    var result = await service.GetAllAsync(page ?? 1, pageSize ?? 10);

    // Devolvemos 'result.Data' que ahora es un objeto PagedResponse (items, total, pages...)
    return Results.Ok(result.Data);
});

// POST /api/v1/products
api.MapPost("/", async (CreateProductRequest request, ProductService service) =>
{
    var result = await service.CreateAsync(request);
    return result.Succeeded
        ? Results.Created($"/api/v1/products/{result.Data!.Id}", result.Data)
        : Results.BadRequest(result.Errors);
});

// POST /api/v1/products/{id}/reduce-stock
api.MapPost("/{id:int}/reduce-stock", async (int id, UpdateStockRequest request, ProductService service) =>
{
    var result = await service.ReduceStockAsync(id, request.Stock);
    return result.Succeeded ? Results.Ok() : Results.BadRequest(result.Errors);
});

// PUT: Editar Producto
api.MapPut("/{id:int}", async (int id, UpdateProductRequest request, ProductService service) =>
{
    var result = await service.UpdateAsync(id, request);
    return result.Succeeded ? Results.Ok(result.Data) : Results.NotFound(result.Errors);
});

// DELETE: Borrar Producto
api.MapDelete("/{id:int}", async (int id, ProductService service) =>
{
    var result = await service.DeleteAsync(id);
    return result.Succeeded ? Results.NoContent() : Results.NotFound(result.Errors);
});

app.MapGet("/", () => Results.Redirect("/scalar/v1"));

app.Run();