using Microsoft.EntityFrameworkCore;
using littleShop.catalog.Data;
using littleShop.catalog.DTOs;
using littleShop.catalog.Entities;
using littleShop.catalog.Shared;

namespace littleShop.catalog.Services;

public class ProductService(CatalogDbContext db)
{
    public async Task<ServiceResult<PagedResponse<ProductResponse>>> GetAllAsync(int page = 1, int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 50) pageSize = 50;

        var totalCount = await db.Products.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var products = await db.Products
            .OrderBy(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            // ✅ AÑADIDO: p.ImageUrl al constructor del DTO
            .Select(p => new ProductResponse(p.Id, p.Name, p.Description, p.Price, p.Stock, p.ImageUrl))
            .ToListAsync();

        var response = new PagedResponse<ProductResponse>(
            products, totalCount, page, pageSize, totalPages
        );

        return ServiceResult<PagedResponse<ProductResponse>>.Success(response);
    }

    public async Task<ServiceResult<ProductResponse>> CreateAsync(CreateProductRequest request)
    {
        var product = new Product
        {
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            Stock = request.Stock,
            ImageUrl = request.ImageUrl // ✅ GUARDAMOS LA URL
        };

        db.Products.Add(product);
        await db.SaveChangesAsync();

        // ✅ DEVOLVEMOS LA URL
        var response = new ProductResponse(product.Id, product.Name, product.Description, product.Price, product.Stock, product.ImageUrl);
        return ServiceResult<ProductResponse>.Success(response);
    }

    public async Task<ServiceResult> ReduceStockAsync(int id, int quantity)
    {
        var product = await db.Products.FindAsync(id);
        if (product is null) return ServiceResult.Failure("Producto no encontrado");
        if (product.Stock < quantity) return ServiceResult.Failure($"No hay suficiente stock. Stock actual: {product.Stock}");

        product.Stock -= quantity;
        await db.SaveChangesAsync();
        return ServiceResult.Success();
    }

    public async Task<ServiceResult<ProductResponse>> UpdateAsync(int id, UpdateProductRequest request)
    {
        var product = await db.Products.FindAsync(id);
        if (product is null) return ServiceResult<ProductResponse>.Failure("Producto no encontrado");

        product.Name = request.Name;
        product.Description = request.Description;
        product.Price = request.Price;
        product.Stock = request.Stock;
        product.ImageUrl = request.ImageUrl; // ✅ ACTUALIZAMOS LA URL

        await db.SaveChangesAsync();

        var response = new ProductResponse(product.Id, product.Name, product.Description, product.Price, product.Stock, product.ImageUrl);
        return ServiceResult<ProductResponse>.Success(response);
    }

    public async Task<ServiceResult> DeleteAsync(int id)
    {
        var product = await db.Products.FindAsync(id);
        if (product is null) return ServiceResult.Failure("Producto no encontrado");

        db.Products.Remove(product);
        await db.SaveChangesAsync();
        return ServiceResult.Success();
    }

    public async Task<ServiceResult<ProductResponse>> GetByIdAsync(int id)
    {
        var product = await db.Products.FindAsync(id);
        if (product == null) return ServiceResult<ProductResponse>.Failure($"Producto {id} no encontrado.");

        // ✅ AÑADIDO ImageUrl
        var response = new ProductResponse(
            product.Id,
            product.Name,
            product.Description,
            product.Price,
            product.Stock,
            product.ImageUrl
        );

        return ServiceResult<ProductResponse>.Success(response);
    }
}