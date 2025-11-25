using Aspire.Hosting;
using System.IO;

var builder = DistributedApplication.CreateBuilder(args);

// --- 1. PostgreSQL (Con pgAdmin) ---
var postgresContainer = builder.AddPostgres("postgres")
    .WithDataVolume("littleshop-postgres-data")
    .WithHostPort(5432)
    .WithLifetime(ContainerLifetime.Persistent)
    // NUEVO: Añadimos la interfaz visual pgAdmin en el puerto 5050
    .WithPgAdmin(pg => pg.WithHostPort(5050));

var littleShopDb = postgresContainer.AddDatabase("littleshop-db");

// --- 2. Redis (NUEVO) ---
// Añadimos Redis para caché y Rate Limiting futuro
var redis = builder.AddRedis("redis")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume("littleshop-redis-data")
    .WithRedisInsight(); // Añade la interfaz visual para ver la caché

// --- 3. Identity Service ---
var identityService = builder.AddProject<Projects.littleShop_identity>("littleshop-identity")
    .WithReference(littleShopDb)
    .WaitFor(littleShopDb)
    .WithHttpEndpoint(name: "identity-http")
    .WithExternalHttpEndpoints();

// --- 4. API GATEWAY ---
var apiGateway = builder.AddProject<Projects.littleshop_apiGateway>("littleshop-apigateway")
    .WithReference(identityService)
    .WaitFor(identityService)
    // NUEVO: El Gateway necesita Redis para poder limitar peticiones más adelante
    .WithReference(redis)
    .WaitFor(redis)
    .WithHttpEndpoint(name: "gateway-http");

// --- 5. Frontend ---
var frontendPath = Path.Combine(builder.AppHostDirectory, "..", "littleshop.frontend");

var frontendApp = builder.AddExecutable("littleshop-frontend", "npm", frontendPath, "run", "dev")
    .WithHttpEndpoint(env: "PORT", name: "frontend-http")
    .WithEnvironment("VITE_IDENTITY_API_URL", $"{apiGateway.GetEndpoint("gateway-http")}/api/identity")
    .WithReference(apiGateway)
    .WaitFor(apiGateway);

builder.Build().Run();