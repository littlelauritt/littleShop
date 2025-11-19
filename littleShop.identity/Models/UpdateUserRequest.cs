#nullable enable

namespace littleShop.identity.Models
{
    public class UpdateUserRequest
    {
        public string Email { get; set; } = string.Empty;
        public string? NewPassword { get; set; }
        public bool? LockoutEnabled { get; set; }
    }
}