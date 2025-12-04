using littleShop.identity.Models;
using littleShop.identity.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Projects.littleShop_identity.Data;
using System.Security.Claims;
using MassTransit;
using littleShop.Shared.Events;
using FluentValidation;
using Microsoft.AspNetCore.WebUtilities; // Necesario para codificar el token de forma segura en URLs
using System.Text;

namespace littleShop.identity.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Tags("🔑 Autenticación (Cuenta y Login)")]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly JwtService _jwtService;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly IValidator<RegisterRequest> _registerValidator;

        public AccountController(
            UserManager<IdentityUser> userManager,
            JwtService jwtService,
            IPublishEndpoint publishEndpoint,
            IValidator<RegisterRequest> registerValidator)
        {
            _userManager = userManager;
            _jwtService = jwtService;
            _publishEndpoint = publishEndpoint;
            _registerValidator = registerValidator;
        }

        // 1. REGISTRO
        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest model)
        {
            // A. Validación de entrada
            var validationResult = await _registerValidator.ValidateAsync(model);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors.Select(e => e.ErrorMessage));
            }

            var user = new IdentityUser
            {
                UserName = model.Email,
                Email = model.Email
            };

            // B. Crear usuario en Identity
            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            // C. Asignar rol por defecto
            await _userManager.AddToRoleAsync(user, Roles.User);

            // --- D. GENERACIÓN DE TOKEN DE CONFIRMACIÓN ---
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            // Codificamos el token para que sea seguro ponerlo en una URL (Base64Url)
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            // --- E. PUBLICAR EVENTO CON EL TOKEN ---
            await _publishEndpoint.Publish(new UserCreatedEvent(
                user.Id,
                user.Email!,
                encodedToken, // Pasamos el token codificado
                DateTime.UtcNow));

            return Ok(new { Message = "Usuario registrado. Por favor, revisa tu email para confirmar la cuenta." });
        }

        // 2. LOGIN
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null || user.Email == null)
                return Unauthorized(new { Message = "Usuario o contraseña incorrecta" });

            // --- F. VALIDACIÓN DE EMAIL CONFIRMADO ---
            if (!await _userManager.IsEmailConfirmedAsync(user))
            {
                return Unauthorized(new { Message = "Debes confirmar tu email antes de iniciar sesión.", Code = "EMAIL_NOT_CONFIRMED" });
            }

            var passwordValid = await _userManager.CheckPasswordAsync(user, model.Password);
            if (!passwordValid)
                return Unauthorized(new { Message = "Usuario o contraseña incorrecta" });

            var roles = await _userManager.GetRolesAsync(user);
            var primaryRole = roles.FirstOrDefault() ?? Roles.User;

            var token = await _jwtService.GenerateJwtAsync(user.Id, user.Email, primaryRole);

            return Ok(token);
        }

        // 3. CONFIRMAR EMAIL (MEJORADO / BLINDADO)
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
                // INTENTO 1: Formato Correcto (Base64Url)
                // Esto funcionará para los usuarios nuevos y URLs limpias
                var decodedBytes = WebEncoders.Base64UrlDecode(request.Code);
                decodedToken = Encoding.UTF8.GetString(decodedBytes);
            }
            catch
            {
                // INTENTO 2: Fallback para formato "Sucio" (Base64 Estándar)
                // Si el token llegó corrupto (ej. espacios en lugar de +), intentamos arreglarlo.
                try
                {
                    var dirtyCode = request.Code.Replace(" ", "+");
                    var decodedBytes = Convert.FromBase64String(dirtyCode);
                    decodedToken = Encoding.UTF8.GetString(decodedBytes);
                }
                catch
                {
                    return BadRequest("El token de verificación está corrupto y no se pudo leer.");
                }
            }

            // Validamos el token con Identity
            var result = await _userManager.ConfirmEmailAsync(user, decodedToken);

            if (result.Succeeded)
            {
                return Ok(new { Message = "Email confirmado correctamente. Ya puedes iniciar sesión." });
            }

            return BadRequest("Error al confirmar el email. El token puede haber expirado o es inválido.");
        }

        // 4. OBTENER USUARIO ACTUAL
        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
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

        // 5. SOLO ADMIN (Ejemplo)
        [Authorize(Roles = Roles.Admin)]
        [HttpGet("admin-only")]
        public IActionResult AdminOnly()
        {
            return Ok(new { Message = "Acceso permitido" });
        }
    }

    // DTO para la confirmación de email
    public class ConfirmEmailRequest
    {
        public required string UserId { get; set; }
        public required string Code { get; set; }
    }
}