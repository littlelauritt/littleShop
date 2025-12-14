using Aspire.Hosting;
using System.IO;

var builder = DistributedApplication.CreateBuilder(args);

// --- 1. FRONTEND ---
var frontendPath = Path.Combine(builder.AppHostDirectory, "..", "littleshop.frontend");
var frontendApp = builder.AddExecutable("littleshop-frontend", "npm", frontendPath, "run", "dev")
    .WithHttpEndpoint(env: "PORT", port: 5173, name: "frontend-http")
    .WithExternalHttpEndpoints();

// --- 2. Infraestructura ---

// VUELVE EL PUERTO 5432 (Como tú querías)
var postgresContainer = builder.AddPostgres("postgres")
    .WithDataVolume("littleshop-postgres-data")
    .WithHostPort(5432) // <--- Aquí está tu puerto fijo recuperado
    .WithLifetime(ContainerLifetime.Persistent)
    .WithPgAdmin(pg => pg.WithHostPort(5050));

// Base de datos para Identity (renombrada correctamente)
var identityDb = postgresContainer.AddDatabase("identitydb");
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
    .WithReference(identityDb).WaitFor(identityDb)
    .WithReference(rabbit).WaitFor(rabbit)
    .WithHttpEndpoint(name: "identity-http")
    .WithExternalHttpEndpoints()
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
    .WithEnvironment("FRONTEND_URL", frontendApp.GetEndpoint("frontend-http"))
    .WithEnvironment("AdminEmail", "admin@littleshop.com");

// --- 5. Gateway ---
var apiGateway = builder.AddProject<Projects.littleshop_apiGateway>("littleshop-apigateway")
    .WithReference(identityService).WaitFor(identityService)
    .WithReference(catalogService).WaitFor(catalogService)
    .WithReference(ordersService).WaitFor(ordersService)
    .WithReference(redis).WaitFor(redis)
    .WithHttpEndpoint(name: "gateway-http");

// Configuración final del frontend
frontendApp
    .WithEnvironment("VITE_GATEWAY_URL", apiGateway.GetEndpoint("gateway-http"))
    .WithReference(apiGateway)
    .WaitFor(apiGateway);

builder.Build().Run();