using littleShop.identity.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace littleShop.identity.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProfileController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;

        public ProfileController(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        // -------------------------------------------------------------
        // MÉTODO CORREGIDO: Usa ClaimTypes.NameIdentifier
        // -------------------------------------------------------------
        [Authorize]
        [HttpGet("me")]
        [SwaggerOperation(Summary = "Obtiene el perfil propio")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null) return Unauthorized(new { Message = "Autenticación fallida o token sin ID." });

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound(new { Message = "Usuario no encontrado en la base de datos." });

            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new { user.Id, user.Email, Roles = roles });
        }

        // -------------------------------------------------------------
        // MÉTODO ROBUSTO: Está correcto, se mantiene
        // -------------------------------------------------------------
        [Authorize]
        [HttpPut("me")]
        [SwaggerOperation(Summary = "Actualiza el perfil propio (Email)")]
        public async Task<IActionResult> UpdateProfile([FromBody] UserProfileUpdateRequest model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null) return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null) return NotFound(new { Message = "Usuario no encontrado en la base de datos." });

            if (user.Email != model.Email)
            {
                user.Email = model.Email;
                user.UserName = model.Email;

                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                    return BadRequest(result.Errors);
            }

            return Ok(new { Message = "Perfil actualizado correctamente" });
        }

        // -------------------------------------------------------------
        // MÉTODO CORREGIDO: Usa ClaimTypes.NameIdentifier
        // -------------------------------------------------------------
        [Authorize]
        [HttpPost("change-password")]
        [SwaggerOperation(Summary = "Cambia la contraseña del usuario actual")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null) return Unauthorized(new { Message = "Autenticación fallida o token sin ID." });

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            // Usar ChangePasswordAsync requiere la contraseña actual y la nueva
            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            return Ok(new { Message = "Contraseña actualizada con éxito" });
        }
    }
}