using Microsoft.EntityFrameworkCore;
using littleShop.catalog.Entities;

namespace littleShop.catalog.Data;

public class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    public virtual DbSet<Product> Products { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Product>().Property(e => e.Name).HasMaxLength(200).IsRequired();
        modelBuilder.Entity<Product>().Property(e => e.Price).HasPrecision(18, 2);
    }
}