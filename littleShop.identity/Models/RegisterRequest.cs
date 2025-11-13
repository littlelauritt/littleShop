namespace littleShop.identity.Models;

public class RegisterRequest
{
    // Estas propiedades son las que tu controlador espera
    public required string Email { get; set; }
    public required string Password { get; set; }
}