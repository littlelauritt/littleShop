using littleShop.identity.Models;

namespace littleShop.identity.Services;

public interface IJwtService
{
    Task<AuthResponse> GenerateJwtAsync(string userId, string email, string role);
}