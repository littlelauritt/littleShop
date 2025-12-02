using Microsoft.AspNetCore.RateLimiting;
using RedisRateLimiting;
using StackExchange.Redis;
using littleshop.serviceDefaults;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// 1. REDIS
builder.AddRedisClient("redis");

// --- NUEVO: 1.1 CONFIGURAR CORS ---
// Esto permite que CUALQUIER origen (tu frontend) se conecte.
// En producción se restringe, pero para desarrollo esto soluciona el problema.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
// ----------------------------------

// 2. RATE LIMITING
builder.Services.AddRateLimiter(rateLimiterOptions =>
{
    rateLimiterOptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    rateLimiterOptions.AddPolicy("anonymous", context =>
    {
        var redis = context.RequestServices.GetRequiredService<IConnectionMultiplexer>();
        // Usamos una clave fija para desarrollo si IP falla, para evitar nulls
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "development-ip";

        return RedisRateLimitPartition.GetFixedWindowRateLimiter(
            $"anon:{ipAddress}",
            _ => new RedisFixedWindowRateLimiterOptions
            {
                ConnectionMultiplexerFactory = () => redis,
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            });
    });

    rateLimiterOptions.AddPolicy("authenticated", context =>
    {
        var redis = context.RequestServices.GetRequiredService<IConnectionMultiplexer>();
        var userKey = context.User.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RedisRateLimitPartition.GetFixedWindowRateLimiter(
            $"auth:{userKey}",
            _ => new RedisFixedWindowRateLimiterOptions
            {
                ConnectionMultiplexerFactory = () => redis,
                PermitLimit = 1000,
                Window = TimeSpan.FromMinutes(1)
            });
    });
});

// 3. YARP
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver();

// 4. JWT
var jwtOptions = builder.Configuration.GetSection("Jwt");
// Agregamos un chequeo de seguridad por si la clave no carga
var secretKey = jwtOptions["Key"] ?? throw new InvalidOperationException("JWT Key not found!");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions["Issuer"],
            ValidAudience = jwtOptions["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        };
    });

// 5. POLÍTICAS DE AUTORIZACIÓN
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("public_access", policy => policy.RequireAssertion(_ => true));
    options.AddPolicy("authenticated", policy => policy.RequireAuthenticatedUser());
    options.AddPolicy("admin", policy => policy.RequireRole("Admin"));
});

var app = builder.Build();

app.MapDefaultEndpoints();

// --- NUEVO: USAR LA POLÍTICA DE CORS ---
// ¡Importante! Debe ir ANTES de Auth y de ReverseProxy
app.UseCors("AllowAll");
// ---------------------------------------

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapReverseProxy();

app.MapGet("/", () => Results.Ok(new { Status = "Healthy", Service = "LittleShop Gateway" }));

app.Run();