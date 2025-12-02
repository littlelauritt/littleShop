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

    public async Task<ServiceResult> ReduceStockAsync(int id, int quantity)
    {
        var product = await db.Products.FindAsync(id);

        if (product is null)
            return ServiceResult.Failure("Producto no encontrado");

        if (product.Stock < quantity)
            return ServiceResult.Failure($"No hay suficiente stock. Stock actual: {product.Stock}");

        // Restamos el stock
        product.Stock -= quantity;
        await db.SaveChangesAsync();

        return ServiceResult.Success();
    }

    // EDITAR PRODUCTO
    public async Task<ServiceResult<ProductResponse>> UpdateAsync(int id, UpdateProductRequest request)
    {
        var product = await db.Products.FindAsync(id);
        if (product is null) return ServiceResult<ProductResponse>.Failure("Producto no encontrado");

        product.Name = request.Name;
        product.Description = request.Description;
        product.Price = request.Price;
        product.Stock = request.Stock;

        await db.SaveChangesAsync();

        var response = new ProductResponse(product.Id, product.Name, product.Description, product.Price, product.Stock);
        return ServiceResult<ProductResponse>.Success(response);
    }

    // BORRAR PRODUCTO
    public async Task<ServiceResult> DeleteAsync(int id)
    {
        var product = await db.Products.FindAsync(id);
        if (product is null) return ServiceResult.Failure("Producto no encontrado");

        db.Products.Remove(product);
        await db.SaveChangesAsync();

        return ServiceResult.Success();
    }
}

