namespace littleShop.catalog.DTOs;

public record ProductResponse(int Id, string Name, string? Description, decimal Price, int Stock);
public record CreateProductRequest(string Name, string? Description, decimal Price, int Stock);