using System.IO;
using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// --- PostgreSQL con base de datos asociada ---
// 1. Define el contenedor del servidor PostgreSQL (la máquina)
var postgresContainer = builder.AddPostgres("postgres")
    .WithDataVolume();

// 2. Define el recurso de base de datos lógica 'littleshop-db' dentro del contenedor
var littleShopDb = postgresContainer.AddDatabase("littleshop-db");

// --- Proyecto de identidad ---
var identityService = builder.AddProject<Projects.littleShop_identity>("littleshop-identity")
    // CORRECTO: Inyecta la referencia del recurso 'littleShopDb'.
    .WithReference(littleShopDb);

// ----------------------------------------------------------------------------------
// CONFIGURACIÓN DE LA APLICACIÓN REACT (littleshop.frontend)
// ----------------------------------------------------------------------------------

// Ruta al directorio de la aplicación React
var frontendPath = Path.Combine(builder.AppHostDirectory, "..", "littleshop.frontend");

// Agrega el proyecto React como un recurso ejecutable
var frontendApp = builder.AddExecutable("littleshop-frontend", "npm", frontendPath, "run", "dev")
    .WithHttpEndpoint(targetPort: 5173, name: "http")
    // Inyecta la URL del endpoint HTTP de Identity en React
    .WithEnvironment("VITE_IDENTITY_API_URL", identityService.GetEndpoint("http"));

frontendApp.WithReference(identityService);

builder.Build().Run();