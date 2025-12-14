using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Projects.littleShop_identity.Data;
using System.IO;

namespace littleShop.identity.Data
{
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            var connectionString = configuration.GetConnectionString("littleshop-db");

            var builder = new DbContextOptionsBuilder<ApplicationDbContext>();
            builder.UseNpgsql(connectionString);

            return new ApplicationDbContext(builder.Options);
        }
    }
}