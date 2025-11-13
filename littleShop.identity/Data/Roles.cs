using System.Collections.Generic;

namespace Projects.littleShop_identity.Data;

// Clase estática para centralizar los nombres de los roles.
public static class Roles
{
    // Constante para el rol de Administrador.
    public const string Admin = nameof(Admin);

    // Constante para el rol de Usuario Estándar.
    public const string User = nameof(User);

    // Devuelve una lista con los nombres de todos los roles definidos.
    public static List<string> GetAvailableRoles()
    {
        return new List<string>
        {
            Admin,
            User
        };
    }
}
