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

        // 1. REGISTRO (Modificado para Email Confirmation)
        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest model)
        {
            // A. Validación de entrada (Formato email, password segura, etc.)
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

            // Codificamos el token para que sea seguro ponerlo en una URL (evita problemas con caracteres especiales como '+')
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            // --- E. PUBLICAR EVENTO CON EL TOKEN ---
            // Enviamos el evento a RabbitMQ para que el servicio de notificaciones envíe el correo
            await _publishEndpoint.Publish(new UserCreatedEvent(
                user.Id,
                user.Email!,
                encodedToken, // Pasamos el token codificado
                DateTime.UtcNow));

            return Ok(new { Message = "Usuario registrado. Por favor, revisa tu email para confirmar la cuenta." });
        }

        // 2. LOGIN (Modificado para bloquear no confirmados)
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null || user.Email == null)
                return Unauthorized(new { Message = "Usuario o contraseña incorrecta" });

            // --- F. VALIDACIÓN DE EMAIL CONFIRMADO ---
            // Si el usuario existe pero no ha confirmado su email, no le dejamos entrar.
            if (!await _userManager.IsEmailConfirmedAsync(user))
            {
                return Unauthorized(new { Message = "Debes confirmar tu email antes de iniciar sesión.", Code = "EMAIL_NOT_CONFIRMED" });
            }
            // -------------------------------------

            var passwordValid = await _userManager.CheckPasswordAsync(user, model.Password);
            if (!passwordValid)
                return Unauthorized(new { Message = "Usuario o contraseña incorrecta" });

            var roles = await _userManager.GetRolesAsync(user);
            var primaryRole = roles.FirstOrDefault() ?? Roles.User;

            var token = await _jwtService.GenerateJwtAsync(user.Id, user.Email, primaryRole);

            return Ok(token);
        }

        // 3. CONFIRMAR EMAIL (Nuevo Endpoint)
        // Este endpoint es llamado por el Frontend cuando el usuario hace clic en el enlace del correo
        [AllowAnonymous]
        [HttpPost("confirm-email")]
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest request)
        {
            var user = await _userManager.FindByIdAsync(request.UserId);
            if (user == null) return NotFound("Usuario no encontrado.");

            // Decodificamos el token que viene de la URL/Frontend
            string decodedToken;
            try
            {
                var decodedBytes = WebEncoders.Base64UrlDecode(request.Code);
                decodedToken = Encoding.UTF8.GetString(decodedBytes);
            }
            catch
            {
                return BadRequest("Token inválido o corrupto.");
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