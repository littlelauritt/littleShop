import React from 'react';
import { Navigate } from 'react-router-dom';
import { jwtDecode } from 'jwt-decode';
import { getToken, logout } from '../../assets/utils/auth';

// Definiciones de claims comunes
// Usaremos estas claves para intentar extraer el rol del token
const NET_ROLE_CLAIM = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';
const OLD_NET_ROLE_CLAIM = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/role';
const SPANISH_ROLE_CLAIM = 'rol';
const PLURAL_ROLES_CLAIM = 'roles';

// Interfaz del Payload del Token
interface JwtPayload {
    sub: string;
    email: string;
    role?: string | string[];
    [NET_ROLE_CLAIM]?: string | string[];
    [OLD_NET_ROLE_CLAIM]?: string | string[];
    [SPANISH_ROLE_CLAIM]?: string | string[];
    [PLURAL_ROLES_CLAIM]?: string[];
    exp: number;
    // Permitir cualquier otra clave desconocida para el debugging
    [key: string]: unknown;
}

interface ProtectedRouteProps {
    children: React.ReactNode;
    allowedRoles: string[];
}

const ProtectedRoute: React.FC<ProtectedRouteProps> = ({ children, allowedRoles }) => {
    const token = getToken();

    if (!token) {
        return <Navigate to="/login" replace />;
    }

    try {
        const decoded = jwtDecode<JwtPayload>(token);

        let userRole: string | undefined;

        // --- FUNCIÓN HELPER PARA EXTRAER EL PRIMER ROL ---
        const getFirstRole = (claimValue: string | string[] | undefined): string | undefined => {
            if (claimValue === undefined) return undefined;
            if (Array.isArray(claimValue)) {
                return claimValue.length > 0 ? claimValue[0] : undefined;
            }
            return claimValue;
        };

        // --- LÓGICA DE DETECCIÓN DE ROL MULTI-CLAIM (Prueba todas las variantes) ---

        // 1. Clave 'role' (singular, inglés)
        userRole = getFirstRole(decoded.role);

        // 2. Clave 'rol' (singular, español)
        if (!userRole) {
            userRole = getFirstRole(decoded[SPANISH_ROLE_CLAIM]);
        }

        // 3. Clave 'roles' (¡PLURAL!)
        if (!userRole) {
            userRole = getFirstRole(decoded[PLURAL_ROLES_CLAIM]);
        }

        // 4. Clave de .NET (URL larga moderna)
        if (!userRole) {
            userRole = getFirstRole(decoded[NET_ROLE_CLAIM]);
        }

        // 5. Clave de .NET (URL larga antigua)
        if (!userRole) {
            userRole = getFirstRole(decoded[OLD_NET_ROLE_CLAIM]);
        }

        // --- FIN LÓGICA DE DETECCIÓN DE ROL ---

        const expirationTime = decoded.exp * 1000;

        // 1. Verificar expiración
        if (Date.now() >= expirationTime) {
            console.warn("Token expirado.");
            logout();
            return <Navigate to="/login" replace />;
        }

        // 2. Verificación de Robustez y Autorización
        if (!userRole || typeof userRole !== 'string') {

            // 🚨 CAMBIO CRÍTICO: Imprime el payload completo en el error.
            const payloadJson = JSON.stringify(decoded, null, 2);
            console.error(
                `[FATAL] ROL FALTANTE. El token decodificado NO contiene ninguna de las claves de rol esperadas (role, rol, roles, o claves de .NET). Payload completo: \n${payloadJson}`
            );

            logout();
            return <Navigate to="/login" replace />;
        }

        // 3. Verificar si el rol detectado está permitido para esta ruta
        if (!allowedRoles.includes(userRole)) {
            console.warn(`Acceso denegado: El rol '${userRole}' no tiene permiso para esta ruta.`);
            // Si es un usuario normal intentando entrar a admin, lo mandamos a su perfil
            return <Navigate to="/profile" replace />;
        }

        console.log(`Éxito: Rol '${userRole}' detectado correctamente. Acceso concedido.`);

    } catch (error) {
        console.error("Error al decodificar/validar el token:", error);
        logout();
        return <Navigate to="/login" replace />;
    }

    // Si todo está bien, renderizar el componente hijo
    return <>{children}</>;
};

export default ProtectedRoute;