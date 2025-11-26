using Aspire.Hosting;
using System.IO;

var builder = DistributedApplication.CreateBuilder(args);

// --- 1. PostgreSQL (Con pgAdmin) ---
var postgresContainer = builder.AddPostgres("postgres")
    .WithDataVolume("littleshop-postgres-data")
    .WithHostPort(5432)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithPgAdmin(pg => pg.WithHostPort(5050));

// Definimos las 3 bases de datos
var littleShopDb = postgresContainer.AddDatabase("littleshop-db"); // Usuarios
var catalogDb = postgresContainer.AddDatabase("catalogdb");        // Productos
var ordersDb = postgresContainer.AddDatabase("ordersdb");          // Pedidos (Futuro)

// --- 2. Redis ---
var redis = builder.AddRedis("redis")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume("littleshop-redis-data")
    .WithRedisInsight();

// --- 3. Identity Service ---
var identityService = builder.AddProject<Projects.littleShop_identity>("littleshop-identity")
    .WithReference(littleShopDb)
    .WaitFor(littleShopDb)
    .WithHttpEndpoint(name: "identity-http")
    .WithExternalHttpEndpoints();

// --- 4. CATALOG SERVICE (Lo ponemos aquí ordenado) ---
// Guardamos la variable 'catalogService' para pasársela luego al Gateway
var catalogService = builder.AddProject<Projects.littleShop_catalog>("littleshop-catalog")
    .WithReference(catalogDb)  // <--- CORRECTO: Usa su propia DB
    .WaitFor(catalogDb)
    .WithHttpEndpoint(name: "catalog-http");

// --- 5. API GATEWAY ---
var apiGateway = builder.AddProject<Projects.littleshop_apiGateway>("littleshop-apigateway")
    // Referencias a servicios
    .WithReference(identityService)
    .WaitFor(identityService)
    .WithReference(catalogService) // <--- ¡IMPORTANTE! Añadido para que espere al catálogo
    .WaitFor(catalogService)
    // Referencia a Redis
    .WithReference(redis)
    .WaitFor(redis)
    .WithHttpEndpoint(name: "gateway-http");

// --- 6. Frontend ---
var frontendPath = Path.Combine(builder.AppHostDirectory, "..", "littleshop.frontend");

var frontendApp = builder.AddExecutable("littleshop-frontend", "npm", frontendPath, "run", "dev")
    .WithHttpEndpoint(env: "PORT", name: "frontend-http")
    .WithEnvironment("VITE_IDENTITY_API_URL", $"{apiGateway.GetEndpoint("gateway-http")}/api/identity")
    .WithReference(apiGateway)
    .WaitFor(apiGateway);


builder.Build().Run();