using Aspire.Npgsql.EntityFrameworkCore.PostgreSQL;
using littleShop.identity.Controllers;
using littleShop.identity.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Projects.littleShop_identity.Data;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// ----------------------------------------------------
// Conexión y Contexto
// ----------------------------------------------------
builder.AddNpgsqlDbContext<ApplicationDbContext>("littleshop-db");

// ----------------------------------------------------
// Identity Setup
// ----------------------------------------------------
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireDigit = true;
    options.Password.RequireNonAlphanumeric = true;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders()
    .AddErrorDescriber<SpanishIdentityErrorDescriber>();

// ----------------------------------------------------
// Servicios y CORS
// ----------------------------------------------------
builder.Services.AddControllers()
    .AddApplicationPart(typeof(AccountController).Assembly);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
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
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();

        await context.Database.MigrateAsync();

        if (!await roleManager.RoleExistsAsync(Roles.Admin))
            await roleManager.CreateAsync(new IdentityRole(Roles.Admin));
        if (!await roleManager.RoleExistsAsync(Roles.User))
            await roleManager.CreateAsync(new IdentityRole(Roles.User));

        app.Logger.LogInformation("Migraciones y Seeding de roles aplicados correctamente.");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Error durante migración o seeding de la base de datos.");
    }
}

// ----------------------------------------------------
// Pipeline HTTP
// ----------------------------------------------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
