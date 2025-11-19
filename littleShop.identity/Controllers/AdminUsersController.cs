using littleShop.identity.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Projects.littleShop_identity.Data;
using Swashbuckle.AspNetCore.Annotations;

namespace littleShop.identity.Controllers
{
    [ApiController]
    [Route("api/admin/users")] // Pequeño cambio en ruta para que sea más RESTful
    [Authorize(Roles = Roles.Admin)]
    public class AdminUsersController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;

        public AdminUsersController(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        [HttpGet]
        [SwaggerOperation(Summary = "Obtiene todos los usuarios (incluyendo estado de bloqueo)")]
        public IActionResult GetAllUsers()
        {
            // Devolvemos también si está bloqueado (LockoutEnd)
            var users = _userManager.Users.Select(u => new {
                u.Id,
                u.Email,
                IsLocked = u.LockoutEnd > DateTimeOffset.UtcNow
            }).ToList();
            return Ok(users);
        }

        [HttpPost]
        [SwaggerOperation(Summary = "Crea un nuevo usuario")]
        public async Task<IActionResult> CreateUser([FromBody] RegisterRequest model)
        {
            var user = new IdentityUser { UserName = model.Email, Email = model.Email };
            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded) return BadRequest(result.Errors);

            await _userManager.AddToRoleAsync(user, Roles.User);
            return Ok(new { Message = "Usuario creado con éxito" });
        }

        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "Obtiene un usuario por ID")]
        public async Task<IActionResult> GetUserById(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            return Ok(new { user.Id, user.Email, Roles = roles, LockoutEnd = user.LockoutEnd });
        }

        [HttpPut("{id}")]
        [SwaggerOperation(Summary = "Actualiza información básica de un usuario (Admin)")]
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
        [SwaggerOperation(Summary = "Elimina un usuario por ID")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded) return BadRequest(result.Errors);

            return Ok(new { Message = "Usuario eliminado" });
        }

        // --- LOCK / UNLOCK ---

        [HttpPost("{id}/lock")]
        [SwaggerOperation(Summary = "Bloquea la cuenta de un usuario temporalmente (100 años)")]
        public async Task<IActionResult> LockUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // Bloquear por 100 años
            var result = await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
            if (!result.Succeeded) return BadRequest(result.Errors);

            return Ok(new { Message = "Usuario bloqueado correctamente" });
        }

        [HttpPost("{id}/unlock")]
        [SwaggerOperation(Summary = "Desbloquea la cuenta de un usuario")]
        public async Task<IActionResult> UnlockUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // Establecer fecha de fin de bloqueo a null (o al pasado) desbloquea al usuario
            var result = await _userManager.SetLockoutEndDateAsync(user, null);
            if (!result.Succeeded) return BadRequest(result.Errors);

            return Ok(new { Message = "Usuario desbloqueado correctamente" });
        }
    }
}