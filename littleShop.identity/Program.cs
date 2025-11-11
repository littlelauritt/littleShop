var builder = WebApplication.CreateBuilder(args);

// Añadir servicios al contenedor
builder.Services.AddControllers();

// Añadir CORS para Aspire/React
builder.Services.AddCors(options =>
{
    // Usamos AllowAnyOrigin() en entornos de desarrollo con Aspire.
    // Aspire se asegura de que solo los servicios orquestados puedan hablar entre sí.
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configurar pipeline HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Activar CORS (¡Va antes de UseAuthorization!)
app.UseCors("AllowFrontend");

app.UseAuthorization();

app.MapControllers();

app.Run();