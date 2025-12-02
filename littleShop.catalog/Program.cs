using littleshop.serviceDefaults;
using littleShop.catalog.Data;
using littleShop.catalog.DTOs;
using littleShop.catalog.Services;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// 1. CONEXIÓN A BASE DE DATOS (Postgres)
builder.AddNpgsqlDbContext<CatalogDbContext>("catalogdb");

// 2. SERVICIOS
builder.Services.AddScoped<ProductService>();

// 3. OPENAPI (SCALAR)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();

app.MapDefaultEndpoints();

// 4. MIGRACIONES AUTOMÁTICAS (Crea tablas al arrancar)
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); // Documentación en /scalar/v1

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    await db.Database.MigrateAsync(); // Esto crea la tabla Products en Postgres
}

// ==================================================================
// 5. DEFINICIÓN DE ENDPOINTS (MINIMAL API)
// ==================================================================

var api = app.MapGroup("/api/v1/products").WithTags("Products");

// GET /api/v1/products
api.MapGet("/", async (ProductService service) =>
{
    var result = await service.GetAllAsync();
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
// Este endpoint lo llamará el microservicio de Orders
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