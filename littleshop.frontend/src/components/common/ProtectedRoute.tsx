import React from 'react';
import { Navigate } from 'react-router-dom';
import { jwtDecode } from 'jwt-decode';
import { getToken, logout } from '../../assets/utils/auth';

// 1. DEFINIMOS LA CLAVE LARGA QUE USA MICROSOFT
const MICROSOFT_ROLE_CLAIM = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";

interface JwtPayload {
    exp: number;
    // Definimos que puede venir 'role' (corto) o la URL larga
    role?: string | string[];
    [MICROSOFT_ROLE_CLAIM]?: string | string[];
    // Permitimos otras propiedades
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

        // --- LÓGICA DE EXTRACCIÓN DE ROL ---

        // A. Intentamos leer la clave larga de Microsoft (Prioridad 1)
        const longClaim = decoded[MICROSOFT_ROLE_CLAIM];

        // B. Intentamos leer la clave corta estándar (Prioridad 2)
        const shortClaim = decoded.role;

        // Decidimos cuál usar
        const roleValue = longClaim || shortClaim;

        if (roleValue) {
            if (Array.isArray(roleValue)) {
                // Si tiene múltiples roles, tomamos el primero (o podrías verificar si ALGUNO coincide)
                // Para simplificar, aquí buscamos si alguno de los roles del usuario está en allowedRoles
                const hasMatchingRole = roleValue.some(r => allowedRoles.includes(r));
                if (hasMatchingRole) {
                    // Si encontramos coincidencia, asignamos un rol válido para pasar la validación de abajo
                    // (Esto es un truco: asignamos el primer rol permitido que encontramos)
                    userRole = roleValue.find(r => allowedRoles.includes(r));
                } else {
                    userRole = roleValue[0]; // Si no coincide ninguno, guardamos el primero para el log de error
                }
            } else {
                // Si es un solo rol (string)
                userRole = roleValue as string;
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
            console.error("Error Crítico: El token es válido pero no se encontró ningún rol (ni corto ni largo).");
            // Imprimimos el token para depurar si vuelve a fallar
            console.log("Token decodificado:", decoded);
            logout();
            return <Navigate to="/login" replace />;
        }

        // 4. Verificar si el rol tiene permiso
        if (!allowedRoles.includes(userRole)) {
            console.warn(`Acceso denegado: El rol '${userRole}' no está en la lista permitida [${allowedRoles.join(', ')}].`);
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