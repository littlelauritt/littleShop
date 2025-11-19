import React from 'react';
import { Navigate } from 'react-router-dom';
import { jwtDecode } from 'jwt-decode';
import { getToken, logout } from '../../assets/utils/auth';

// Ahora sabemos con certeza que la clave es 'role'
const ROLE_CLAIM = 'role';

interface JwtPayload {
    exp: number;
    [ROLE_CLAIM]?: string | string[]; // Puede ser string o array de strings
    [key: string]: unknown;
}

interface ProtectedRouteProps {
    children: React.ReactNode;
    allowedRoles: string[];
}

const ProtectedRoute: React.FC<ProtectedRouteProps> = ({ children, allowedRoles }) => {
    const token = getToken();

    // 1. Si no hay token, al login
    if (!token) {
        return <Navigate to="/login" replace />;
    }

    try {
        const decoded = jwtDecode<JwtPayload>(token);
        let userRole: string | undefined;

        // --- Extracción de Rol (Lógica Limpia) ---
        const roleClaimValue = decoded[ROLE_CLAIM];

        if (roleClaimValue) {
            if (Array.isArray(roleClaimValue)) {
                // Si el usuario tiene múltiples roles, tomamos el primero
                userRole = roleClaimValue[0];
            } else {
                // Si es un solo rol (string)
                userRole = roleClaimValue as string;
            }
        }

        const expirationTime = decoded.exp * 1000;

        // 2. Verificar expiración
        if (Date.now() >= expirationTime) {
            console.warn("Token expirado.");
            logout();
            return <Navigate to="/login" replace />;
        }

        // 3. Verificación de existencia del Rol
        if (!userRole) {
            console.error("Error Crítico: El token es válido pero no contiene el campo 'role'.");
            logout();
            return <Navigate to="/login" replace />;
        }

        // 4. Verificar si el rol tiene permiso
        if (!allowedRoles.includes(userRole)) {
            console.warn(`Acceso denegado: El rol '${userRole}' no tiene permiso aquí.`);
            // Si es Admin intentando entrar, o User intentando entrar a Admin
            return <Navigate to="/profile" replace />;
        }

    } catch (error) {
        console.error("Error al procesar el token:", error);
        logout();
        return <Navigate to="/login" replace />;
    }

    // 5. ¡Acceso Concedido!
    return <>{children}</>;
};

export default ProtectedRoute;