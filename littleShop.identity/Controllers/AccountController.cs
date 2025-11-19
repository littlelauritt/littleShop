using littleShop.identity.Models;
using littleShop.identity.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Projects.littleShop_identity.Data;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace littleShop.identity.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly JwtService _jwtService;

        public AccountController(UserManager<IdentityUser> userManager, JwtService jwtService)
        {
            _userManager = userManager;
            _jwtService = jwtService;
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest model)
        {
            var user = new IdentityUser
            {
                UserName = model.Email,
                Email = model.Email
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            await _userManager.AddToRoleAsync(user, Roles.User);

            return Ok(new { Message = "Usuario registrado con éxito" });
        }

        [AllowAnonymous]
        [HttpPost("login")]
        [SwaggerOperation(Summary = "Login de usuario y obtención de JWT")] // Asegúrate de tener la anotación
        public async Task<IActionResult> Login([FromBody] LoginRequest model)
        {
            // 1. Buscar al usuario por Email/UserName
            // Usamos FindByEmailAsync, ya que el usuario es más propenso a recordarlo.
            var user = await _userManager.FindByEmailAsync(model.Email);

            // 2. Si no existe, Unauthorized (para no dar pistas sobre qué falla)
            if (user == null)
                return Unauthorized(new { Message = "Usuario o contraseña incorrecta" });

            // 3. Verificar la contraseña
            var passwordValid = await _userManager.CheckPasswordAsync(user, model.Password);
            if (!passwordValid)
                return Unauthorized(new { Message = "Usuario o contraseña incorrecta" });

            // 4. Generar el Token JWT
            var roles = await _userManager.GetRolesAsync(user);

            // Asumimos que el primer rol es el que se quiere incluir en el token.
            var token = await _jwtService.GenerateJwtAsync(user.Id, user.Email, roles.First());

            return Ok(token);
        }

        [Authorize]
        [HttpGet("me")]
        [SwaggerOperation(Summary = "Obtiene información del usuario autenticado")]
        public async Task<IActionResult> GetCurrentUser()
        {
            // Usar el ID del Claim para mayor robustez
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
                return Unauthorized(); // Token no tiene ID

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound(); // Usuario no existe en DB

            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new
            {
                user.Id,
                user.Email,
                Roles = roles
            });
        }

        [Authorize(Roles = Roles.Admin)]
        [HttpGet("admin-only")]
        public IActionResult AdminOnly()
        {
            return Ok(new { Message = "Acceso permitido" });
        }
    }
}

