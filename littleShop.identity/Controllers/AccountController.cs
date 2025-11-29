using littleShop.identity.Models;
using littleShop.identity.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Projects.littleShop_identity.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.Metadata;
using MassTransit; // <--- USAMOS MASSTRANSIT
using littleShop.Shared.Events;
using FluentValidation;

namespace littleShop.identity.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Tags("🔑 Autenticación (Cuenta y Login)")]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly JwtService _jwtService;

        // CAMBIO 1: Usamos IPublishEndpoint en vez de IConnection
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly IValidator<RegisterRequest> _registerValidator;

        public AccountController(
            UserManager<IdentityUser> userManager,
            JwtService jwtService,
            IPublishEndpoint publishEndpoint, // <--- INYECTADO
            IValidator<RegisterRequest> registerValidator) // <--- INYECTADO
        {
            _userManager = userManager;
            _jwtService = jwtService;
            _publishEndpoint = publishEndpoint;
            _registerValidator = registerValidator;
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest model)
        {
            // CAMBIO 2: Validación con FluentValidation
            var validationResult = await _registerValidator.ValidateAsync(model);
            if (!validationResult.IsValid)
            {
                // Devolvemos los errores de forma limpia
                return BadRequest(validationResult.Errors.Select(e => e.ErrorMessage));
            }

            var user = new IdentityUser
            {
                UserName = model.Email,
                Email = model.Email
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            await _userManager.AddToRoleAsync(user, Roles.User);

            // ============================================================
            // CAMBIO 3: ENVIAR EVENTO CON MASSTRANSIT (Súper limpio)
            // ============================================================
            // Fíjate que esto sustituye a las 20 líneas de código nativo de antes
            await _publishEndpoint.Publish(new UserCreatedEvent(
                user.Id,
                user.Email!,
                DateTime.UtcNow));

            // ============================================================

            return Ok(new { Message = "Usuario registrado con éxito" });
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest model)
        {
            // ESTE MÉTODO SE QUEDA EXACTAMENTE IGUAL QUE LO TENÍAS
            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null || user.Email == null)
                return Unauthorized(new { Message = "Usuario o contraseña incorrecta" });

            var passwordValid = await _userManager.CheckPasswordAsync(user, model.Password);
            if (!passwordValid)
                return Unauthorized(new { Message = "Usuario o contraseña incorrecta" });

            var roles = await _userManager.GetRolesAsync(user);
            var primaryRole = roles.FirstOrDefault() ?? Roles.User;

            var token = await _jwtService.GenerateJwtAsync(user.Id, user.Email, primaryRole);

            return Ok(token);
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            // ESTE MÉTODO SE QUEDA EXACTAMENTE IGUAL QUE LO TENÍAS
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null) return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

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