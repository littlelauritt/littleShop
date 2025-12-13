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

// 1. CONEXIÓN A BASE DE DATOS
builder.AddNpgsqlDbContext<CatalogDbContext>("catalogdb");

// 2. SERVICIOS
builder.Services.AddScoped<ProductService>();

// 3. CONFIGURACIÓN MASSTRANSIT
builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();
    x.AddConsumer<OrderCancelledConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var configuration = context.GetRequiredService<IConfiguration>();
        var connectionString = configuration.GetConnectionString("messaging");

        if (!string.IsNullOrEmpty(connectionString))
        {
            cfg.Host(new Uri(connectionString));
        }
        else
        {
            cfg.Host("messaging", "/");
        }
        cfg.ConfigureEndpoints(context);
    });
});

// 4. OPENAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();

app.MapDefaultEndpoints();

// =========================================================
// 5. MIGRACIONES Y DATA SEEDING (CRÍTICO PARA DOCKER)
// =========================================================
// Lo hacemos fuera del 'if Development' para que se ejecute siempre
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        // Esto crea la tabla si no existe
        await db.Database.MigrateAsync();
        Console.WriteLine("✅ Base de datos de Catálogo migrada correctamente.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ Error migrando DB Catálogo (Puede que Postgres aún no esté listo): {ex.Message}");
    }
}

// 6. DOCUMENTACIÓN (SCALAR) - DISPONIBLE SIEMPRE
app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.WithTitle("LittleShop Catalog API");
    options.WithTheme(ScalarTheme.Mars); // ¡Le ponemos un tema diferente para distinguirla!
    options.WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
});


// =========================================================
// DEFINICIÓN DE ENDPOINTS (MINIMAL API)
// =========================================================

var api = app.MapGroup("/api/v1/products").WithTags("Products");

api.MapGet("/", async (int? page, int? pageSize, ProductService service) =>
{
    var result = await service.GetAllAsync(page ?? 1, pageSize ?? 10);
    return Results.Ok(result.Data);
});

api.MapPost("/", async (CreateProductRequest request, ProductService service) =>
{
    var result = await service.CreateAsync(request);
    return result.Succeeded
        ? Results.Created($"/api/v1/products/{result.Data!.Id}", result.Data)
        : Results.BadRequest(result.Errors);
});

api.MapPost("/{id:int}/reduce-stock", async (int id, UpdateStockRequest request, ProductService service) =>
{
    var result = await service.ReduceStockAsync(id, request.Stock);
    return result.Succeeded ? Results.Ok() : Results.BadRequest(result.Errors);
});

api.MapPut("/{id:int}", async (int id, UpdateProductRequest request, ProductService service) =>
{
    var result = await service.UpdateAsync(id, request);
    return result.Succeeded ? Results.Ok(result.Data) : Results.NotFound(result.Errors);
});

api.MapDelete("/{id:int}", async (int id, ProductService service) =>
{
    var result = await service.DeleteAsync(id);
    return result.Succeeded ? Results.NoContent() : Results.NotFound(result.Errors);
});

// Redirección a la documentación
app.MapGet("/", () => Results.Redirect("/scalar/v1"));

app.Run();