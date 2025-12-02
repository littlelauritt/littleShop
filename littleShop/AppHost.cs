using Aspire.Hosting;
using System.IO;

var builder = DistributedApplication.CreateBuilder(args);

// --- 1. PostgreSQL ---
var postgresContainer = builder.AddPostgres("postgres")
    .WithDataVolume("littleshop-postgres-data")
    .WithHostPort(5432)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithPgAdmin(pg => pg.WithHostPort(5050));

var littleShopDb = postgresContainer.AddDatabase("littleshop-db");
var catalogDb = postgresContainer.AddDatabase("catalogdb");
var ordersDb = postgresContainer.AddDatabase("ordersdb");

// --- 2. RabbitMQ ---
var rabbit = builder.AddRabbitMQ("messaging")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithManagementPlugin();

// --- 3. Redis ---
var redis = builder.AddRedis("redis")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume("littleshop-redis-data")
    .WithRedisInsight();

// --- NUEVO: MAILDEV (Servidor SMTP falso) ---
var maildev = builder.AddContainer("maildev", "maildev/maildev")
    .WithHttpEndpoint(targetPort: 1080, name: "maildev-dashboard") // Para ver los emails en web
    .WithEndpoint(targetPort: 1025, name: "smtp"); // Puerto para enviar correos

// --- 4. Identity ---
var identityService = builder.AddProject<Projects.littleShop_identity>("littleshop-identity")
    .WithReference(littleShopDb)
    .WaitFor(littleShopDb)
    .WithReference(rabbit) // Identity envía mensajes
    .WaitFor(rabbit)
    .WithHttpEndpoint(name: "identity-http")
    .WithExternalHttpEndpoints();

// --- 5. Catalog ---
var catalogService = builder.AddProject<Projects.littleShop_catalog>("littleshop-catalog")
    .WithReference(catalogDb)
    .WaitFor(catalogDb)
    .WithHttpEndpoint(name: "catalog-http");

// --- 6. Orders ---
var ordersService = builder.AddProject<Projects.littleShop_orders>("littleshop-orders")
    .WithReference(ordersDb)
    .WaitFor(ordersDb)
    .WithReference(catalogService)
    .WaitFor(catalogService)
    .WithReference(rabbit)
    .WaitFor(rabbit)
    .WithHttpEndpoint(name: "orders-http");

// --- 7. NOTIFICATIONS WORKER (¡CORREGIDO!) ---
builder.AddProject<Projects.littleShop_notifications>("littleshop-notifications")
    .WithReference(rabbit).WaitFor(rabbit)
    //.WithReference(maildev)
    .WithEnvironment("SMTP_HOST", maildev.GetEndpoint("smtp"));

// --- 8. Gateway ---
var apiGateway = builder.AddProject<Projects.littleshop_apiGateway>("littleshop-apigateway")
    .WithReference(identityService).WaitFor(identityService)
    .WithReference(catalogService).WaitFor(catalogService)
    .WithReference(ordersService).WaitFor(ordersService)
    .WithReference(redis).WaitFor(redis)
    .WithHttpEndpoint(name: "gateway-http");

// --- 9. Frontend ---
var frontendPath = Path.Combine(builder.AppHostDirectory, "..", "littleshop.frontend");
var frontendApp = builder.AddExecutable("littleshop-frontend", "npm", frontendPath, "run", "dev")
    .WithHttpEndpoint(env: "PORT", name: "frontend-http")
    .WithEnvironment("VITE_GATEWAY_URL", apiGateway.GetEndpoint("gateway-http"))
    .WithReference(apiGateway)
    .WaitFor(apiGateway);

builder.Build().Run();