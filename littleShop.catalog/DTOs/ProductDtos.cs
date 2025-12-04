using System.ComponentModel.DataAnnotations;

namespace littleShop.catalog.DTOs;

public record ProductResponse(int Id, string Name, string? Description, decimal Price, int Stock);

public record CreateProductRequest(
    [Required][MinLength(3)] string Name,
    string? Description,
    [Range(0.01, 10000)] decimal Price,
    [Range(0, 9999)] int Stock
);

public record UpdateStockRequest(int Stock);
public record UpdateProductRequest(string Name, string? Description, decimal Price, int Stock);

public record PagedResponse<T>(
    IEnumerable<T> Items,
    int TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages
);