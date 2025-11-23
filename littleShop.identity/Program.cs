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
using Swashbuckle.AspNetCore.SwaggerUI;
using System.Reflection;
using Scalar.AspNetCore;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

// 1. BASE DE DATOS
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("littleshop-db")));

// 2. IDENTITY
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireDigit = true;
    options.Password.RequireNonAlphanumeric = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// 3. OPENAPI NATIVO (V1)
builder.Services.AddOpenApi("v1", options =>
{
    // A. DEFINIR EL ESQUEMA (DocumentTransformer)
    // Aquí solo decimos "Existe un esquema llamado Bearer", pero no lo aplicamos a nadie todavía.
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info.Title = "LittleShop Identity API V1";
        document.Info.Version = "v1";

        var securityScheme = new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Description = "JWT Authorization header using the Bearer scheme.",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Reference = new OpenApiReference
            {
                Id = "Bearer",
                Type = ReferenceType.SecurityScheme
            }
        };

        document.Components ??= new OpenApiComponents();

        document.Components.SecuritySchemes ??= new Dictionary<string, OpenApiSecurityScheme>();

        
        var schemes = document.Components.SecuritySchemes;

        if (!schemes.ContainsKey("Bearer"))
        {
            schemes.Add("Bearer", securityScheme);
        }

        return Task.CompletedTask;
    });

    // B. APLICAR EL CANDADO CONDICIONALMENTE (OperationTransformer)
    // Esto se ejecuta por cada endpoint (Controller/Action).
    options.AddOperationTransformer((operation, context, cancellationToken) =>
    {
        var metadata = context.Description.ActionDescriptor.EndpointMetadata;

        // Verificamos si tiene [Authorize] y NO tiene [AllowAnonymous]
        bool hasAuthorize = metadata.OfType<Microsoft.AspNetCore.Authorization.IAuthorizeData>().Any();
        bool hasAllowAnonymous = metadata.OfType<Microsoft.AspNetCore.Authorization.IAllowAnonymous>().Any();

        if (hasAuthorize && !hasAllowAnonymous)
        {
            // Agregamos el requisito de seguridad SOLO a esta operación
            operation.Security = new List<OpenApiSecurityRequirement>
            {
                new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer" // Debe coincidir con el Id definido arriba
                            }
                        },
                        Array.Empty<string>()
                    }
                }
            };

            // Opcional: Indicar en la respuesta que puede devolver 401
            operation.Responses ??= new OpenApiResponses();
            operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Unauthorized" });
        }

        return Task.CompletedTask;
    });
});

// 4. JWT & AUTH
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

        // BORRA O COMENTA ESTAS LÍNEAS SI LAS PUSISTE:
        // RoleClaimType = "role"  <-- ¡FUERA! Ya no hace falta.
    };
});

builder.Services.AddAuthorization();

// 5. VERSIONADO
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

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

builder.Services.AddControllers();

var app = builder.Build();

// 6. MIGRACIÓN DB + SEED DE ROLES AUTOMÁTICO
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

    // Aplica migraciones pendientes
    await context.Database.MigrateAsync();

    // Roles que queremos asegurar que existan
    string[] roles = new[] { "Admin", "User" };

    foreach (var roleName in roles)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            var result = await roleManager.CreateAsync(new IdentityRole(roleName));
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new Exception($"No se pudo crear el rol '{roleName}': {errors}");
            }
        }
    }
}

// 7. PIPELINE HTTP
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

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "LittleShop Identity V1");
    });
}

app.UseHttpsRedirection();
app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/", () => Results.Redirect("/scalar/v1"));
app.MapGet("/help", () => Results.Ok(new
{
    Scalar = "/scalar/v1",
    Swagger = "/swagger/index.html"
}));

app.Run();
