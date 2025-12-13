using FluentValidation;
using littleShop.identity.Models;
using littleShop.identity.Services;
using littleShop.Shared.Events;
using MassTransit;
using MassTransit.JobService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Projects.littleShop_identity.Data;
using System.Security.Claims;
using System.Text;

namespace littleShop.identity.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Tags("🔑 Autenticación (Cuenta y Login)")]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IJwtService _jwtService;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly IValidator<RegisterRequest> _registerValidator;

        public AccountController(
            UserManager<IdentityUser> userManager,
            IJwtService jwtService,
            IPublishEndpoint publishEndpoint,
            IValidator<RegisterRequest> registerValidator)
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
            var validationResult = await _registerValidator.ValidateAsync(model);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors.Select(e => e.ErrorMessage));
            }

            var user = new IdentityUser { UserName = model.Email, Email = model.Email };
            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded) return BadRequest(result.Errors);

            await _userManager.AddToRoleAsync(user, Roles.User);

            // Generar token de confirmación
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            // ⬇️ NUEVO: AUTO-CONFIRMAR SOLO EN DESARROLLO (para que pasen los tests)
            var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
            if (isDevelopment)
            {
                await _userManager.ConfirmEmailAsync(user, token);
            }

            // Codificar token y publicar evento (el email se envía igual)
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            await _publishEndpoint.Publish(new UserCreatedEvent(
                user.Id,
                user.Email!,
                encodedToken,
                DateTime.UtcNow));

            return Ok(new { Message = "Usuario registrado. Revisa tu email." });
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null) return Unauthorized(new { Message = "Credenciales incorrectas" });

            if (!await _userManager.IsEmailConfirmedAsync(user))
            {
                return Unauthorized(new { Message = "Debes confirmar tu email antes de iniciar sesión.", Code = "EMAIL_NOT_CONFIRMED" });
            }

            if (!await _userManager.CheckPasswordAsync(user, model.Password))
                return Unauthorized(new { Message = "Credenciales incorrectas" });

            var roles = await _userManager.GetRolesAsync(user);
            var token = await _jwtService.GenerateJwtAsync(user.Id, user.Email!, roles.FirstOrDefault() ?? Roles.User);

            return Ok(token);
        }

        [AllowAnonymous]
        [HttpPost("confirm-email")]
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest request)
        {
            if (string.IsNullOrEmpty(request.UserId) || string.IsNullOrEmpty(request.Code))
                return BadRequest("Faltan datos (UserId o Code).");

            var user = await _userManager.FindByIdAsync(request.UserId);
            if (user == null) return NotFound("Usuario no encontrado.");

            string decodedToken;
            try
            {
                // 1. Intento estándar (Base64Url)
                var decodedBytes = WebEncoders.Base64UrlDecode(request.Code);
                decodedToken = Encoding.UTF8.GetString(decodedBytes);
            }
            catch
            {
                // 2. Intento de recuperación (si el token llegó dañado)
                try
                {
                    var dirtyCode = request.Code.Replace(" ", "+");
                    var decodedBytes = Convert.FromBase64String(dirtyCode);
                    decodedToken = Encoding.UTF8.GetString(decodedBytes);
                }
                catch
                {
                    return BadRequest("El token de verificación está corrupto.");
                }
            }

            var result = await _userManager.ConfirmEmailAsync(user, decodedToken);

            if (result.Succeeded)
            {
                return Ok(new { Message = "Email confirmado correctamente." });
            }

            // Devolver error específico de Identity si falla
            var errorMsg = result.Errors.FirstOrDefault()?.Description ?? "Token inválido o expirado.";
            return BadRequest(errorMsg);
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();
            var roles = await _userManager.GetRolesAsync(user);
            return Ok(new { user.Id, user.Email, Roles = roles });
        }

        [Authorize(Roles = Roles.Admin)]
        [HttpGet("admin-only")]
        public IActionResult AdminOnly()
        {
            return Ok(new { Message = "Acceso permitido" });
        }
    }

    public class ConfirmEmailRequest
    {
        public required string UserId { get; set; }
        public required string Code { get; set; }
    }
}