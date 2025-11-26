using littleShop.identity.Models;
using littleShop.identity.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Projects.littleShop_identity.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.Metadata;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using littleShop.identity.Events; 

namespace littleShop.identity.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Tags("🔑 Autenticación (Cuenta y Login)")]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly JwtService _jwtService;

        // 1. NUEVA VARIABLE PARA LA CONEXIÓN RABBITMQ
        private readonly IConnection _rabbitConnection;

        // 2. INYECTAMOS LA CONEXIÓN EN EL CONSTRUCTOR
        public AccountController(
            UserManager<IdentityUser> userManager,
            JwtService jwtService,
            IConnection rabbitConnection) // <--- AÑADIDO
        {
            _userManager = userManager;
            _jwtService = jwtService;
            _rabbitConnection = rabbitConnection; // <--- GUARDADO
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

            // ============================================================
            // 3. CÓDIGO PARA ENVIAR EVENTO A RABBITMQ
            // ============================================================
            try
            {
                // 1. Crear el canal (AHORA ES ASÍNCRONO)
                using var channel = await _rabbitConnection.CreateChannelAsync();

                // 2. Declarar la cola (AHORA ES ASÍNCRONO)
                await channel.QueueDeclareAsync(
                    queue: "user-created",
                    durable: false,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                // 3. Preparar el mensaje
                var eventData = new UserCreatedEvent(user.Id, user.Email!, DateTime.UtcNow);
                var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(eventData));

                // 4. Publicar (AHORA ES ASÍNCRONO y cambia un poco la firma)
                // Nota: BasicProperties es obligatorio, usamos uno vacío
                var props = new RabbitMQ.Client.BasicProperties();

                await channel.BasicPublishAsync(
                    exchange: "",
                    routingKey: "user-created",
                    mandatory: false,
                    basicProperties: props,
                    body: body);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error enviando evento a RabbitMQ: {ex.Message}");
            }
            // ============================================================

            return Ok(new { Message = "Usuario registrado con éxito" });
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest model)
        {
            // 1. Buscar al usuario por Email
            var user = await _userManager.FindByEmailAsync(model.Email);

            // 2. Validaciones básicas
            if (user == null || user.Email == null)
                return Unauthorized(new { Message = "Usuario o contraseña incorrecta" });

            // 3. Verificar la contraseña
            var passwordValid = await _userManager.CheckPasswordAsync(user, model.Password);
            if (!passwordValid)
                return Unauthorized(new { Message = "Usuario o contraseña incorrecta" });

            // 4. Generar el Token JWT
            var roles = await _userManager.GetRolesAsync(user);

            // Obtenemos el rol principal o "User" por defecto
            var primaryRole = roles.FirstOrDefault() ?? Roles.User;

            // Generamos el token
            var token = await _jwtService.GenerateJwtAsync(user.Id, user.Email, primaryRole);

            return Ok(token);
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
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