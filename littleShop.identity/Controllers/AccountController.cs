using littleShop.identity.Models; 
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Projects.littleShop_identity.Data;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace littleShop.identity.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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

        
        [HttpPost("register")]
        
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

          
            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
               
                try
                {
                 
                    var resultOperation= await _userManager.AddToRoleAsync(user, Roles.User);

                    _logger.LogInformation("Usuario registrado y rol asignado con éxito.");
                    return Ok(new { Message = "Usuario registrado y almacenado en la base de datos con éxito." });
                }
                catch (Exception ex)
                {
                   
                    _logger.LogError(ex, "Error al asignar el rol al nuevo usuario.");
                    // Opcional: podrías eliminar el usuario creado aquí si falló la asignación de rol.
                    // await _userManager.DeleteAsync(user); 
                    return StatusCode(500, new { Message = "Error interno al asignar roles." });
                }
            }
            else
            {
   
                _logger.LogWarning("Fallo en el registro de usuario para {Email}: {Errors}", model.Email, string.Join(", ", result.Errors.Select(e => e.Description)));

             
                var errors = result.Errors.Select(e => new { Code = e.Code, Message = e.Description });
                return BadRequest(new { Message = "Fallo en el registro", Errors = errors });
            }
        }

    }
}