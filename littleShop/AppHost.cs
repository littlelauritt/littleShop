using System.IO;
using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// --- PostgreSQL con base de datos asociada ---
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .AddDatabase("littleshop-db"); 

// --- Proyecto de identidad ---
var identityService = builder.AddProject<Projects.littleShop_identity>("littleshop-identity")
    .WithReference(postgres);

// ----------------------------------------------------------------------------------
// CONFIGURACIÓN DE LA APLICACIÓN REACT (littleshop.frontend)
// ----------------------------------------------------------------------------------

// Ruta al directorio de la aplicación React
var frontendPath = Path.Combine(builder.AppHostDirectory, "..", "littleshop.frontend");

// Agrega el proyecto React como un recurso ejecutable
builder.AddExecutable("littleshop-frontend", "npm", frontendPath, "run", "dev")
       .WithHttpEndpoint(targetPort: 5173, name: "http")
       .WithReference(identityService);

builder.Build().Run();
