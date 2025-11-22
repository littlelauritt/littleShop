using littleShop.identity.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Projects.littleShop_identity.Data;
using Microsoft.AspNetCore.Http.Metadata;

namespace littleShop.identity.Controllers
{

    [ApiController]
    [Route("api/admin/users")]
    [Authorize(Roles = Roles.Admin)]
    [Tags("👤 Administración de Usuarios")]
    public class AdminUsersController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;

        public AdminUsersController(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }
        [HttpGet]
        public IActionResult GetAllUsers()
        {
            // Devolvemos también si está bloqueado (LockoutEnd)
            var users = _userManager.Users.Select(u => new
            {
                u.Id,
                u.Email,
                IsLocked = u.LockoutEnd > DateTimeOffset.UtcNow
            }).ToList();
            return Ok(users);
        }


        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] RegisterRequest model)
        {
            var user = new IdentityUser { UserName = model.Email, Email = model.Email };
            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded) return BadRequest(result.Errors);

            await _userManager.AddToRoleAsync(user, Roles.User);
            return Ok(new { Message = "Usuario creado con éxito" });
        }

  
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            return Ok(new { user.Id, user.Email, Roles = roles, LockoutEnd = user.LockoutEnd });
        }

      
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(string id, [FromBody] AdminUpdateUserRequest model)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            user.Email = model.Email;
            user.UserName = model.Email; // Manteniendo coherencia Email = UserName

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded) return BadRequest(result.Errors);

            return Ok(new { Message = "Usuario actualizado" });
        }

      
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded) return BadRequest(result.Errors);

            return Ok(new { Message = "Usuario eliminado" });
        }

     
        [HttpPost("{id}/lock")]
        public async Task<IActionResult> LockUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            // Bloquear por 100 años
            var result = await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok(new { Message = "Usuario bloqueado correctamente" });
        }

        [HttpPost("{id}/unlock")]
        public async Task<IActionResult> UnlockUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            // Establecer fecha de fin de bloqueo a null desbloquea al usuario
            var result = await _userManager.SetLockoutEndDateAsync(user, null);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok(new { Message = "Usuario desbloqueado correctamente" });
        }
    }
}