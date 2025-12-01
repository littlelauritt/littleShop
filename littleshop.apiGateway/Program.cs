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

// 2. RATE LIMITING (Definimos las políticas que usa el profesor)
builder.Services.AddRateLimiter(rateLimiterOptions =>
{
    rateLimiterOptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    // Política "anonymous": Para rutas públicas (Login/Register)
    rateLimiterOptions.AddPolicy("anonymous", context =>
    {
        var redis = context.RequestServices.GetRequiredService<IConnectionMultiplexer>();
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RedisRateLimitPartition.GetFixedWindowRateLimiter(
            $"anon:{ipAddress}",
            _ => new RedisFixedWindowRateLimiterOptions
            {
                ConnectionMultiplexerFactory = () => redis,
                PermitLimit = 100, // Límite estricto
                Window = TimeSpan.FromMinutes(1)
            });
    });

    // Política "authenticated": Para usuarios logueados
    rateLimiterOptions.AddPolicy("authenticated", context =>
    {
        var redis = context.RequestServices.GetRequiredService<IConnectionMultiplexer>();
        // Usamos el nombre de usuario o la IP si falla
        var userKey = context.User.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString();

        return RedisRateLimitPartition.GetFixedWindowRateLimiter(
            $"auth:{userKey}",
            _ => new RedisFixedWindowRateLimiterOptions
            {
                ConnectionMultiplexerFactory = () => redis,
                PermitLimit = 1000, // Límite relajado
                Window = TimeSpan.FromMinutes(1)
            });
    });
});

// 3. YARP
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver();

// 4. JWT (Para que el Gateway sepa leer tokens)
var jwtOptions = builder.Configuration.GetSection("Jwt");
var secretKey = jwtOptions["Key"];

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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!))
        };
    });

// 5. POLÍTICAS DE AUTORIZACIÓN (Lo que pedía el profesor)
builder.Services.AddAuthorization(options =>
{
    // "anonymous": Deja pasar a cualquiera (Login/Register)
    options.AddPolicy("public_access", policy => policy.RequireAssertion(_ => true));

    // "authenticated": Requiere token válido
    options.AddPolicy("authenticated", policy => policy.RequireAuthenticatedUser());

    // "admin": Requiere token Y rol de Admin
    options.AddPolicy("admin", policy => policy.RequireRole("Admin"));
});

var app = builder.Build();

app.MapDefaultEndpoints();

// MIDDLEWARES
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapReverseProxy();

app.MapGet("/", () => Results.Ok(new { Status = "Healthy", Service = "LittleShop Gateway" }));

app.Run();