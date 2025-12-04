using Aspire.Hosting;
using System.IO;

var builder = DistributedApplication.CreateBuilder(args);

// --- 1. FRONTEND ---
// Definimos el frontend primero para tener su referencia disponible
var frontendPath = Path.Combine(builder.AppHostDirectory, "..", "littleshop.frontend");
var frontendApp = builder.AddExecutable("littleshop-frontend", "npm", frontendPath, "run", "dev")
    // CAMBIO CRÍTICO: Añadimos 'port: 5173' para FIJAR el puerto externo.
    // Aspire reservará el 5173 y se lo pasará a Vite via la variable "PORT".
    .WithHttpEndpoint(env: "PORT", port: 5173, name: "frontend-http")
    .WithExternalHttpEndpoints();

// --- 2. Infraestructura ---
var postgresContainer = builder.AddPostgres("postgres")
    .WithDataVolume("littleshop-postgres-data")
    .WithHostPort(5432)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithPgAdmin(pg => pg.WithHostPort(5050));

var littleShopDb = postgresContainer.AddDatabase("littleshop-db");
var catalogDb = postgresContainer.AddDatabase("catalogdb");
var ordersDb = postgresContainer.AddDatabase("ordersdb");

var rabbit = builder.AddRabbitMQ("messaging")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithManagementPlugin();

var redis = builder.AddRedis("redis")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume("littleshop-redis-data")
    .WithRedisInsight();

var maildev = builder.AddContainer("maildev", "maildev/maildev")
    .WithHttpEndpoint(targetPort: 1080, name: "maildev-dashboard")
    .WithEndpoint(targetPort: 1025, name: "smtp");

// --- 3. Servicios Backend ---

var identityService = builder.AddProject<Projects.littleShop_identity>("littleshop-identity")
    .WithReference(littleShopDb).WaitFor(littleShopDb)
    .WithReference(rabbit).WaitFor(rabbit)
    .WithHttpEndpoint(name: "identity-http")
    .WithExternalHttpEndpoints()
    // Pasamos la URL (que ahora será siempre localhost:5173)
    .WithEnvironment("FRONTEND_URL", frontendApp.GetEndpoint("frontend-http"));

var catalogService = builder.AddProject<Projects.littleShop_catalog>("littleshop-catalog")
    .WithReference(catalogDb).WaitFor(catalogDb)
    .WithReference(rabbit).WaitFor(rabbit)
    .WithHttpEndpoint(name: "catalog-http");

var ordersService = builder.AddProject<Projects.littleShop_orders>("littleshop-orders")
    .WithReference(ordersDb).WaitFor(ordersDb)
    .WithReference(catalogService).WaitFor(catalogService)
    .WithReference(rabbit).WaitFor(rabbit)
    .WithHttpEndpoint(name: "orders-http");

// --- 4. NOTIFICACIONES ---
builder.AddProject<Projects.littleShop_notifications>("littleshop-notifications")
    .WithReference(rabbit).WaitFor(rabbit)
    .WithEnvironment("SMTP_HOST", maildev.GetEndpoint("smtp"))
    // Inyectamos la URL. Al haber fijado el puerto arriba, GetEndpoint devolverá http://localhost:5173
    .WithEnvironment("FRONTEND_URL", frontendApp.GetEndpoint("frontend-http"));

// --- 5. Gateway ---
var apiGateway = builder.AddProject<Projects.littleshop_apiGateway>("littleshop-apigateway")
    .WithReference(identityService).WaitFor(identityService)
    .WithReference(catalogService).WaitFor(catalogService)
    .WithReference(ordersService).WaitFor(ordersService)
    .WithReference(redis).WaitFor(redis)
    .WithHttpEndpoint(name: "gateway-http");

// Configuración final del frontend para que sepa dónde está el Gateway
frontendApp
    .WithEnvironment("VITE_GATEWAY_URL", apiGateway.GetEndpoint("gateway-http"))
    .WithReference(apiGateway)
    .WaitFor(apiGateway);

builder.Build().Run();