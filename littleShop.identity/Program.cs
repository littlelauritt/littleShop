using Asp.Versioning;
using FluentValidation;
using littleshop.serviceDefaults;
using littleShop.identity.Models;
using littleShop.identity.Services;
using MassTransit;
using MassTransit.JobService;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Projects.littleShop_identity.Data;
using Scalar.AspNetCore;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
// 1. CONFIGURACIÓN RABBITMQ (MassTransit)

builder.Services.AddMassTransit(bus =>
{
    bus.SetKebabCaseEndpointNameFormatter();

    bus.UsingRabbitMq((context, cfg) =>
    {
        var configuration = context.GetRequiredService<IConfiguration>();
        var connectionString = configuration.GetConnectionString("messaging");

        if (!string.IsNullOrEmpty(connectionString))
        {
            cfg.Host(new Uri(connectionString));
        }
        else
        {
            cfg.Host("messaging", "/");
        }
    });
});

// 2. CONFIGURACIÓN FLUENT VALIDATION
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// 3. BASE DE DATOS E IDENTITY

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("identitydb")));

builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireDigit = true;
    options.Password.RequireNonAlphanumeric = true;
    options.SignIn.RequireConfirmedEmail = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// 4. OPENAPI (Corregido para .NET 10)
builder.Services.AddOpenApi("v1", options =>
{
    options.AddDocumentTransformer<IdentityDocumentTransformer>();
});

// 5. JWT & AUTH
var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()!;
builder.Services.AddSingleton(jwtOptions);
builder.Services.AddScoped<IJwtService, JwtService>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
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

// 6. VERSIONADO
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

// 7. CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy => { policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod(); });
});

builder.Services.AddControllers();

var app = builder.Build();

app.MapDefaultEndpoints();

// 8. MIDDLEWARES

using (var scope = app.Services.CreateScope())
{
    try
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
    catch (Exception)
    {
        Console.WriteLine("⚠️ BD no disponible (Docker mode).");
    }
}

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapOpenApi();

app.MapScalarApiReference(options =>
{
    options.WithOpenApiRoutePattern("/openapi/v1.json");
    options.WithTitle("LittleShop Identity API");
    options.WithTheme(ScalarTheme.DeepSpace);
});

app.MapGet("/", () => Results.Redirect("/scalar/v1"));

app.Run();

internal sealed class IdentityDocumentTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Info = new OpenApiInfo
        {
            Title = "LittleShop Identity API V1",
            Version = "v1"
        };
        return Task.CompletedTask;
    }
}