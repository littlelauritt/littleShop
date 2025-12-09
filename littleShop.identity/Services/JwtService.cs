using littleShop.identity.Models;
using MassTransit.JobService;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Projects.littleShop_identity.Data;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace littleShop.identity.Services
{
    public class JwtService : IJwtService
    {
        private readonly IConfiguration _config;
        private readonly ApplicationDbContext _context;

        public JwtService(IConfiguration config, ApplicationDbContext context)
        {
            _config = config;
            _context = context;
        }

        public async Task<AuthResponse> GenerateJwtAsync(string userId, string email, string role)
        {
            var jwtOptions = _config.GetSection("Jwt").Get<JwtOptions>()!;
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
{
                // 1. CAMBIO: Usar ClaimTypes.NameIdentifier en lugar de JwtRegisteredClaimNames.Sub
                // Esto asegura que User.FindFirstValue(ClaimTypes.NameIdentifier) funcione siempre.
                new Claim(ClaimTypes.NameIdentifier, userId),
    
                // 2. Usar ClaimTypes.Email (Opcional, pero recomendado para consistencia)
                new Claim(ClaimTypes.Email, email),

                // 3. CAMBIO CRÍTICO: Usar ClaimTypes.Role
                // Esto genera el claim largo: "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
                // Al hacer esto, [Authorize(Roles="Admin")] funcionará automáticamente.
                new Claim(ClaimTypes.Role, role),

                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var expires = DateTime.UtcNow.AddMinutes(jwtOptions.AccessTokenExpirationMinutes);

            var token = new JwtSecurityToken(
                issuer: jwtOptions.Issuer,
                audience: jwtOptions.Audience,
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            // Crear refresh token persistente
            var refreshToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                Token = Guid.NewGuid().ToString(),
                UserId = Guid.Parse(userId),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(jwtOptions.RefreshTokenExpirationDays),
                IsRevoked = false
            };

            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            return new AuthResponse
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                RefreshToken = refreshToken.Token,
                ExpiresAt = expires
            };
        }
    }
}