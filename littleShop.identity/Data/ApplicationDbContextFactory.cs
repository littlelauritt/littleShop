using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Projects.littleShop_identity.Data;
using System.IO;

namespace littleShop.identity.Data
{
    // Esta clase permite que los comandos de 'dotnet ef' (como migrations add)
    // encuentren la cadena de conexión en tiempo de diseño (fuera del AppHost de Aspire).
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            // 1. Obtener la configuración del proyecto (incluyendo appsettings.json)
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            // 2. Extraer la cadena de conexión del archivo appsettings.json
            // NOTA: Esta cadena es SOLO para el tiempo de diseño (ej. migrations). 
            // Aspire proporcionará la cadena real en tiempo de ejecución.
            var connectionString = configuration.GetConnectionString("littleshop-db");

            // 3. Configurar el DbContext para usar la cadena de conexión
            var builder = new DbContextOptionsBuilder<ApplicationDbContext>();
            builder.UseNpgsql(connectionString);

            // 4. Devolver la instancia del contexto.
            return new ApplicationDbContext(builder.Options);
        }
    }
}