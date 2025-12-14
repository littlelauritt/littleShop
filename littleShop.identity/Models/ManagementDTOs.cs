namespace littleShop.identity.Models
{
    public class RoleCreateRequest
    {
        public required string RoleName { get; set; }
    }

    public class RoleUpdateRequest
    {
        public required string NewRoleName { get; set; }
    }

    public class AdminUpdateUserRequest
    {
        public required string Email { get; set; }
    }

    public class UserProfileUpdateRequest
    {
        public required string Email { get; set; }
    }
}