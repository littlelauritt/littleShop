// ESTO ES EL ORQUESTADOR
using Aspire.Hosting;
using System.IO;

var builder = DistributedApplication.CreateBuilder(args);

// --- PostgreSQL con contraseña fija ---
var postgresContainer = builder.AddPostgres("postgres")
    .WithDataVolume("littleshop-postgres-data")
    .WithHostPort(5432)
    .WithLifetime(ContainerLifetime.Persistent);
 //.WithEnvironment("POSTGRES_PASSWORD", "ze9UwkHTC~p{+G*jJ*v)7{");


// Añadimos la base de datos (solo nombre)
var littleShopDb = postgresContainer.AddDatabase("littleshop-db");

// --- Identity ---
var identityService = builder.AddProject<Projects.littleShop_identity>("littleshop-identity")
    .WithReference(littleShopDb)
    .WaitFor(littleShopDb)
    .WithHttpEndpoint(name: "identity-http");

// --- Frontend React ---
var frontendPath = Path.Combine(builder.AppHostDirectory, "..", "littleshop.frontend");
var frontendApp = builder.AddExecutable("littleshop-frontend", "npm", frontendPath, "run", "dev")
    .WithHttpEndpoint(env: "PORT", name: "frontend-http")
    .WithEnvironment("VITE_IDENTITY_API_URL", identityService.GetEndpoint("https"));

frontendApp.WithReference(identityService);

builder.AddProject<Projects.littleshop_apiGateway>("littleshop-apigateway");

builder.Build().Run();
