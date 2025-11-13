using littleShop.identity.Models; // << CONFIRMA que RegisterRequest está en este namespace.
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Projects.littleShop_identity.Data; // << AGREGADO: Para acceder a la clase Roles
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace littleShop.identity.Controllers
{
    // Usamos el decorador [ApiController] para manejar respuestas HTTP automáticamente.
    [ApiController]
    [Route("[controller]")]
    // Aseguramos que solo se apliquen las políticas CORS necesarias.
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Maintainability", "CA1506:Avoid excessive class coupling", Justification = "Controller dependencies are necessary for Identity.")]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            SignInManager<IdentityUser> signInManager,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        /// <summary>
        /// Registra un nuevo usuario en el sistema.
        /// </summary>
        [HttpPost("register")]
        // ** SOLUCIÓN DE AMBIGÜEDAD: Especificamos el namespace completo **
        public async Task<IActionResult> Register([FromBody] littleShop.identity.Models.RegisterRequest model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = new IdentityUser
            {
                UserName = model.Email,
                Email = model.Email
            };

            // Intenta crear el usuario
            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                // La creación del usuario fue exitosa.
                try
                {
                    // Asignar el rol de Usuario Estándar (Roles.User)
                    await _userManager.AddToRoleAsync(user, Roles.User);

                    // Si todo es exitoso, devolvemos 200 OK.
                    // Si el proceso llega aquí, el usuario DEBE estar en la base de datos.
                    _logger.LogInformation("Usuario registrado y rol asignado con éxito.");
                    return Ok(new { Message = "Usuario registrado y almacenado en la base de datos con éxito." });
                }
                catch (Exception ex)
                {
                    // Si falla la asignación del rol, logueamos el error y devolvemos un 500.
                    _logger.LogError(ex, "Error al asignar el rol al nuevo usuario.");
                    // Opcional: podrías eliminar el usuario creado aquí si falló la asignación de rol.
                    // await _userManager.DeleteAsync(user); 
                    return StatusCode(500, new { Message = "Error interno al asignar roles." });
                }
            }
            else
            {
                // La creación del usuario FALLÓ. 
                // Esto incluye el error "DuplicateUserName" (que era nuestra inconsistencia).
                _logger.LogWarning("Fallo en el registro de usuario para {Email}: {Errors}", model.Email, string.Join(", ", result.Errors.Select(e => e.Description)));

                // Formateamos los errores de Identity para la respuesta 400.
                var errors = result.Errors.Select(e => new { Code = e.Code, Message = e.Description });
                return BadRequest(new { Message = "Fallo en el registro", Errors = errors });
            }
        }

        // Más endpoints (Login, etc.) irían aquí...
    }
}