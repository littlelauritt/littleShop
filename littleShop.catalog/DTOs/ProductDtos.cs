using System.ComponentModel.DataAnnotations;

namespace littleShop.catalog.DTOs;

// DTO para devolver datos al cliente
public record ProductResponse(int Id, string Name, string? Description, decimal Price, int Stock);

// DTO para crear productos (con validaciones)
public record CreateProductRequest(
    [Required][MinLength(3)] string Name,
    string? Description,
    [Range(0.01, 10000)] decimal Price,
    [Range(0, 9999)] int Stock
);

// DTO para actualizar stock (¡ESTE ES EL QUE FALTABA!)
public record UpdateStockRequest(int Stock);
// DTO para editar producto
public record UpdateProductRequest(string Name, string? Description, decimal Price, int Stock);