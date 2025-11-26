using Aspire.Hosting;
using System.IO;

var builder = DistributedApplication.CreateBuilder(args);

// --- PostgreSQL (Con pgAdmin) ---
var postgresContainer = builder.AddPostgres("postgres")
    .WithDataVolume("littleshop-postgres-data")
    .WithHostPort(5432)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithPgAdmin(pg => pg.WithHostPort(5050));

// Definimos las 3 bases de datos
var littleShopDb = postgresContainer.AddDatabase("littleshop-db"); // Usuarios
var catalogDb = postgresContainer.AddDatabase("catalogdb");        // Productos
var ordersDb = postgresContainer.AddDatabase("ordersdb");          // Pedidos (Futuro)

// --- RABBITMQ ---
var rabbit = builder.AddRabbitMQ("messaging")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithManagementPlugin();

// --- Redis ---
var redis = builder.AddRedis("redis")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume("littleshop-redis-data")
    .WithRedisInsight();

// --- Identity Service ---
var identityService = builder.AddProject<Projects.littleShop_identity>("littleshop-identity")
    .WithReference(littleShopDb)
    .WaitFor(littleShopDb)
    .WithReference(rabbit)
    .WaitFor(rabbit)
    .WithHttpEndpoint(name: "identity-http")
    .WithExternalHttpEndpoints();

// --- CATALOG SERVICE ---
var catalogService = builder.AddProject<Projects.littleShop_catalog>("littleshop-catalog")
    .WithReference(catalogDb)
    .WaitFor(catalogDb)
    .WithHttpEndpoint(name: "catalog-http");

// --- ORDERS SERVICE  ---
// Lo definimos ANTES del Gateway para poder pasárselo como referencia
var ordersService = builder.AddProject<Projects.littleShop_orders>("littleshop-orders")
    .WithReference(ordersDb)
    .WaitFor(ordersDb)
    .WithHttpEndpoint(name: "orders-http");

// --- API GATEWAY ---
var apiGateway = builder.AddProject<Projects.littleshop_apiGateway>("littleshop-apigateway")
    // Referencias a servicios
    .WithReference(identityService)
    .WaitFor(identityService)
    .WithReference(catalogService)
    .WaitFor(catalogService)
    .WithReference(ordersService) // <--- Ahora sí funciona porque ya está declarado arriba
    .WaitFor(ordersService)
    // Referencia a Redis
    .WithReference(redis)
    .WaitFor(redis)
    .WithHttpEndpoint(name: "gateway-http");

// --- Frontend ---
var frontendPath = Path.Combine(builder.AppHostDirectory, "..", "littleshop.frontend");

var frontendApp = builder.AddExecutable("littleshop-frontend", "npm", frontendPath, "run", "dev")
    .WithHttpEndpoint(env: "PORT", name: "frontend-http")
    .WithEnvironment("VITE_IDENTITY_API_URL", $"{apiGateway.GetEndpoint("gateway-http")}/api/identity")
    .WithReference(apiGateway)
    .WaitFor(apiGateway);

builder.Build().Run();