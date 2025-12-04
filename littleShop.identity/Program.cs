using littleShop.identity.Models;
using littleShop.identity.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Projects.littleShop_identity.Data;
using System.Text;
using System.Reflection;
using Scalar.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using littleshop.serviceDefaults;
using FluentValidation;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// ---------------------------------------------------------
// 1. CONFIGURACIÓN RABBITMQ (MassTransit)
// ---------------------------------------------------------
builder.Services.AddMassTransit(bus =>
{
    bus.SetKebabCaseEndpointNameFormatter();

    bus.UsingRabbitMq((context, cfg) =>
    {
        // Usamos la cadena de conexión completa que Aspire nos da
        var configuration = context.GetRequiredService<IConfiguration>();
        var connectionString = configuration.GetConnectionString("messaging");

        if (!string.IsNullOrEmpty(connectionString))
        {
            cfg.Host(new Uri(connectionString));
        }
        else
        {
            // Fallback por si acaso (aunque no debería entrar aquí)
            cfg.Host("messaging", "/");
        }
    });
});

// ---------------------------------------------------------
// 2. CONFIGURACIÓN FLUENT VALIDATION
// ---------------------------------------------------------
// Escanea el proyecto y registra todos los validadores (como el que acabamos de crear)
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// ---------------------------------------------------------
// 3. BASE DE DATOS E IDENTITY
// ---------------------------------------------------------
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    // CAMBIO IMPORTANTE: "identitydb" debe coincidir con el nombre en AppHost.cs
    options.UseNpgsql(builder.Configuration.GetConnectionString("identitydb")));

builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireDigit = true;
    options.Password.RequireNonAlphanumeric = true;
    options.SignIn.RequireConfirmedEmail = true; // Aseguramos que sea necesario confirmar email
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// ---------------------------------------------------------
// 4. OPENAPI (Simplificado para compilar en .NET 10)
// ---------------------------------------------------------
builder.Services.AddOpenApi("v1", options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info.Title = "LittleShop Identity API V1";
        document.Info.Version = "v1";
        return Task.CompletedTask;
    });
});

// ---------------------------------------------------------
// 5. JWT & AUTH
// ---------------------------------------------------------
var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()!;
builder.Services.AddSingleton(jwtOptions);
builder.Services.AddScoped<JwtService>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer("Bearer", options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtOptions.Issuer,
        ValidAudience = jwtOptions.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key!)),
    };
});

builder.Services.AddAuthorization();

// ---------------------------------------------------------
// 6. VERSIONADO
// ---------------------------------------------------------
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
})
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// ---------------------------------------------------------
// 7. CORS
// ---------------------------------------------------------
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy => { policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod(); });
});

builder.Services.AddControllers();

var app = builder.Build();

app.MapDefaultEndpoints();

// ---------------------------------------------------------
// 8. MIDDLEWARES
// ---------------------------------------------------------

// Migraciones automáticas
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    await context.Database.MigrateAsync();
    string[] roles = new[] { "Admin", "User" };
    foreach (var roleName in roles)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
            await roleManager.CreateAsync(new IdentityRole(roleName));
    }
}

// Documentación (Scalar)
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("LittleShop Identity API")
            .WithTheme(ScalarTheme.DeepSpace)
            .WithOpenApiRoutePattern("/openapi/v1.json");
    });
}

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/", () => Results.Redirect("/scalar/v1"));

app.Run();