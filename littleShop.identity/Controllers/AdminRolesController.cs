using littleShop.identity.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Projects.littleShop_identity.Data;
using Microsoft.AspNetCore.Http.Metadata;

namespace littleShop.identity.Controllers
{

    [ApiController]
    [Route("api/admin/roles")]
    [Authorize(Roles = Roles.Admin)]
    [Tags("🛡️ Administración de Roles")]
    public class AdminRolesController : ControllerBase
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<IdentityUser> _userManager;

        public AdminRolesController(RoleManager<IdentityRole> roleManager, UserManager<IdentityUser> userManager)
        {
            _roleManager = roleManager;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllRoles()
        {
            var roles = await _roleManager.Roles.Select(r => new { r.Id, r.Name }).ToListAsync();
            return Ok(roles);
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> GetRoleById(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role == null) return NotFound(new { Message = "Rol no encontrado" });
            return Ok(new { role.Id, role.Name });
        }

        [HttpPost]
        public async Task<IActionResult> CreateRole([FromBody] RoleCreateRequest model)
        {
            if (await _roleManager.RoleExistsAsync(model.RoleName))
                return BadRequest(new { Message = "El rol ya existe" });

            var result = await _roleManager.CreateAsync(new IdentityRole(model.RoleName));
            if (!result.Succeeded) return BadRequest(result.Errors);

            return Ok(new { Message = $"Rol '{model.RoleName}' creado correctamente" });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRole(string id, [FromBody] RoleUpdateRequest model)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role == null) return NotFound(new { Message = "Rol no encontrado" });

            if (await _roleManager.RoleExistsAsync(model.NewRoleName))
                return BadRequest(new { Message = "Ya existe otro rol con ese nombre" });

            role.Name = model.NewRoleName;
            var result = await _roleManager.UpdateAsync(role);
            if (!result.Succeeded) return BadRequest(result.Errors);

            return Ok(new { Message = "Rol actualizado correctamente" });
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRole(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role == null) return NotFound(new { Message = "Rol no encontrado" });

            // Protección para no borrar el rol Admin por accidente
            if (role.Name == Roles.Admin)
                return BadRequest(new { Message = "No se puede eliminar el rol de Administrador del sistema" });

            var result = await _roleManager.DeleteAsync(role);
            if (!result.Succeeded) return BadRequest(result.Errors);

            return Ok(new { Message = "Rol eliminado correctamente" });
        }


        [HttpGet("{roleName}/users")]
        public async Task<IActionResult> GetUsersInRole(string roleName)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
                return NotFound(new { Message = "El rol no existe" });

            var users = await _userManager.GetUsersInRoleAsync(roleName);
            return Ok(users.Select(u => new { u.Id, u.Email }));
        }


        [HttpPost("{roleName}/assign/{userId}")]
        public async Task<IActionResult> AssignRole(string roleName, string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound(new { Message = "Usuario no encontrado" });

            // IMPORTANTE: Validamos contra la BD
            if (!await _roleManager.RoleExistsAsync(roleName))
                return BadRequest(new { Message = "El rol no existe en la base de datos" });

            var result = await _userManager.AddToRoleAsync(user, roleName);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok(new { Message = $"Rol {roleName} asignado al usuario {user.Email}" });
        }


        [HttpPost("{roleName}/remove/{userId}")]
        public async Task<IActionResult> RemoveRole(string roleName, string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound(new { Message = "Usuario no encontrado" });

            var result = await _userManager.RemoveFromRoleAsync(user, roleName);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok(new { Message = $"Rol {roleName} retirado del usuario {user.Email}" });
        }
    }
}