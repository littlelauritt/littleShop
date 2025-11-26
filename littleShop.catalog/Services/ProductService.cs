using Microsoft.EntityFrameworkCore;
using littleShop.catalog.Data;
using littleShop.catalog.DTOs;
using littleShop.catalog.Entities;
using littleShop.catalog.Shared;

namespace littleShop.catalog.Services;

public class ProductService(CatalogDbContext db)
{
    public async Task<ServiceResult<IEnumerable<ProductResponse>>> GetAllAsync()
    {
        var products = await db.Products
            .Select(p => new ProductResponse(p.Id, p.Name, p.Description, p.Price, p.Stock))
            .ToListAsync();
        return ServiceResult<IEnumerable<ProductResponse>>.Success(products);
    }

    public async Task<ServiceResult<ProductResponse>> CreateAsync(CreateProductRequest request)
    {
        var product = new Product
        {
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            Stock = request.Stock
        };

        db.Products.Add(product);
        await db.SaveChangesAsync();

        var response = new ProductResponse(product.Id, product.Name, product.Description, product.Price, product.Stock);
        return ServiceResult<ProductResponse>.Success(response);
    }
}