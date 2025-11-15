// ESTO ES EL ORQUESTADOR
using Aspire.Hosting;
using System.IO;

var builder = DistributedApplication.CreateBuilder(args);

// --- PostgreSQL ---
var postgresContainer = builder.AddPostgres("postgres")
    .WithHostPort(5432)
    .WithLifetime(ContainerLifetime.Persistent);

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

builder.Build().Run();
