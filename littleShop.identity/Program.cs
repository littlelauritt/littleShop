using System.Reflection; // Ya lo tienes
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Projects.littleShop_identity.Data;
using Aspire.Npgsql.EntityFrameworkCore.PostgreSQL;
using littleShop.identity.Controllers;

var builder = WebApplication.CreateBuilder(args);

// ----------------------------------------------------
// Conexión y Contexto
// ----------------------------------------------------
// Aspire inyecta la cadena de conexión del recurso 'littleshop-db'.
builder.AddNpgsqlDbContext<ApplicationDbContext>("littleshop-db");


// ----------------------------------------------------
// Identity Setup
// ----------------------------------------------------
builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// ----------------------------------------------------
// Servicios y CORS
// ----------------------------------------------------
builder.Services.AddControllers()
    .AddApplicationPart(typeof(AccountController).Assembly);

// Configuración de CORS para permitir la comunicación con el frontend.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        // En desarrollo, permitimos cualquier origen (puerto dinámico) 
        // para evitar que Aspire bloquee CORS.
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();

        // Nota: Si usas cookies de autenticación, necesitarías: 
        // .SetIsOriginAllowed(origin => true)
        // .AllowCredentials()
        // ...pero por ahora, AnyOrigin es la solución más rápida.
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ----------------------------------------------------
// Construcción de la app
// ----------------------------------------------------
var app = builder.Build();

// ----------------------------------------------------
// Inicialización de la base de datos y roles (Seeding)
// ----------------------------------------------------
// Aplicar migraciones y seedear roles (esto debe ejecutarse una sola vez)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();

        // Paso CRÍTICO: Asegura que la base de datos existe y aplica todas las migraciones pendientes.
        // Esto es vital en el ambiente de contenedores de Aspire.
        await context.Database.MigrateAsync();

        // Siembra de Roles (Seeding)
        // **CORREGIDO: Usamos Roles.Admin en lugar de Roles.Administrator**
        if (!await roleManager.RoleExistsAsync(Roles.Admin))
        {
            await roleManager.CreateAsync(new IdentityRole(Roles.Admin));
        }
        if (!await roleManager.RoleExistsAsync(Roles.User))
        {
            await roleManager.CreateAsync(new IdentityRole(Roles.User));
        }

        // Puedes añadir un usuario Admin de prueba aquí si lo necesitas

        app.Logger.LogInformation("Migraciones y Seeding de roles aplicados correctamente.");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Ocurrió un error durante la migración o el seeding de la base de datos.");
    }
}

// ----------------------------------------------------
// Configurar pipeline HTTP
// ----------------------------------------------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();

// 1. AÑADIR UseRouting - Obliga a la aplicación a habilitar el ruteo de endpoints
app.UseRouting();

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

// 2. Usar MapControllers DENTRO del pipeline, DEPUÉS de UseAuthorization
app.MapControllers();

app.Run();