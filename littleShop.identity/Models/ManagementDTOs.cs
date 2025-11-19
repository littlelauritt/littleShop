namespace littleShop.identity.Models
{
    // Para crear un rol nuevo
    public class RoleCreateRequest
    {
        public required string RoleName { get; set; }
    }

    // Para actualizar el nombre de un rol
    public class RoleUpdateRequest
    {
        public required string NewRoleName { get; set; }
    }

    // Para que el Admin edite un usuario
    public class AdminUpdateUserRequest
    {
        public required string Email { get; set; }
        // Puedes añadir aquí Nombre, Telefono, etc.
    }

    // Para que el Usuario se edite a sí mismo
    public class UserProfileUpdateRequest
    {
        public required string Email { get; set; }
    }
}