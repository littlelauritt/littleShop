using littleShop.identity.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.Metadata;

namespace littleShop.identity.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    [Tags("⚙️ Perfil del Usuario")]
    public class ProfileController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;

        public ProfileController(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

       
        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
                return Unauthorized(new { Message = "Autenticación fallida o token sin ID." });

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound(new { Message = "Usuario no encontrado en la base de datos." });

            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new { user.Id, user.Email, Roles = roles });
        }

        
        [Authorize]
        [HttpPut("me")]
        public async Task<IActionResult> UpdateProfile([FromBody] UserProfileUpdateRequest model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
                return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return NotFound(new { Message = "Usuario no encontrado en la base de datos." });

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

       
        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
                return Unauthorized(new { Message = "Autenticación fallida o token sin ID." });

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            return Ok(new { Message = "Contraseña actualizada con éxito" });
        }
    }
}