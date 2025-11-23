using Aspire.Hosting;
using System.IO;

var builder = DistributedApplication.CreateBuilder(args);

// --- 1. PostgreSQL ---
var postgresContainer = builder.AddPostgres("postgres")
    .WithDataVolume("littleshop-postgres-data")
    .WithHostPort(5432)
    .WithLifetime(ContainerLifetime.Persistent);

var littleShopDb = postgresContainer.AddDatabase("littleshop-db");

// --- 2. Identity Service ---
var identityService = builder.AddProject<Projects.littleShop_identity>("littleshop-identity")
    .WithReference(littleShopDb)
    .WaitFor(littleShopDb)
    .WithHttpEndpoint(name: "identity-http")
    .WithExternalHttpEndpoints();

// --- 3. API GATEWAY (¡NUEVO!) ---
// Este es el intermediario. Necesita conocer a Identity para redirigirle el tráfico.
var apiGateway = builder.AddProject<Projects.littleshop_apiGateway>("littleshop-apigateway")
    .WithReference(identityService)  // Para que el Gateway encuentre la IP de Identity
    .WaitFor(identityService)        // Espera a que Identity esté listo
    .WithHttpEndpoint(name: "gateway-http"); // El puerto único para el mundo exterior

// --- 4. Frontend ---
var frontendPath = Path.Combine(builder.AppHostDirectory, "..", "littleshop.frontend");

var frontendApp = builder.AddExecutable("littleshop-frontend", "npm", frontendPath, "run", "dev")
    .WithHttpEndpoint(env: "PORT", name: "frontend-http")
    // --- CAMBIO CLAVE AQUÍ ---
    // Antes apuntábamos directo a identityService. 
    // Ahora apuntamos al Gateway + el prefijo de ruta que definimos en appsettings (/api/identity)
    .WithEnvironment("VITE_IDENTITY_API_URL", $"{apiGateway.GetEndpoint("gateway-http")}/api/identity")
    .WithReference(apiGateway) // El frontend ahora depende del Gateway
    .WaitFor(apiGateway);      // Espera a que el Gateway arranque

builder.Build().Run();