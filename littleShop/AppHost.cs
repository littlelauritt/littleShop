using System.IO;
using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// --- Base de Datos y Servicios de .NET ---
var postgres = builder.AddPostgres("postgres");

// Referencia al proyecto de identidad/API
var identityService = builder.AddProject<Projects.littleShop_identity>("littleshop-identity")
    .WithReference(postgres);

// ----------------------------------------------------------------------------------
// CONFIGURACIÓN DE LA APLICACIÓN REACT (littleshop.frontend)
// ----------------------------------------------------------------------------------

// Define la ruta al directorio de la aplicación React.
// CORRECCIÓN: Usamos 'builder.AppHostDirectory' en lugar de 'builder.Environment.AppHostDirectory'.
var frontendProjectPath = Path.Combine(builder.AppHostDirectory, "..", "littleshop.frontend");

// Agrega el proyecto React como un ejecutable de Aspire.
// Esto le dice a Aspire que ejecute "npm start" en ese directorio.
builder.AddExecutable("littleshop-frontend", "npm", frontendProjectPath, "start")
       // Aspire asignará un puerto HTTP para el frontend.
       .WithHttpEndpoint(targetPort: 3000, name: "http")
       // Referencia al servicio de API.
       .WithReference(identityService);
// Ya no necesitamos .AsResource()

builder.Build().Run();