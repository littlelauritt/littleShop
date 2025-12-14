using System.Collections.Generic;

namespace Projects.littleShop_identity.Data;

public static class Roles
{
    public const string Admin = nameof(Admin);

    public const string User = nameof(User);

    public static List<string> GetAvailableRoles()
    {
        return new List<string>
        {
            Admin,
            User
        };
    }
}
