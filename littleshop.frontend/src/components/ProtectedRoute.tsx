import React from 'react';
import { Navigate } from 'react-router-dom';
import { jwtDecode } from 'jwt-decode';
import { getToken, logout } from '../assets/utils/auth';

const NET_ROLE_CLAIM = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';
const OLD_NET_ROLE_CLAIM = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/role';
const SPANISH_ROLE_CLAIM = 'rol';
const PLURAL_ROLES_CLAIM = 'roles';


interface JwtPayload {
    sub: string;
    email: string;
    role?: string | string[];
    [NET_ROLE_CLAIM]?: string | string[];
    [OLD_NET_ROLE_CLAIM]?: string | string[];
    [SPANISH_ROLE_CLAIM]?: string | string[];
    [PLURAL_ROLES_CLAIM]?: string[];
    exp: number;
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

        const getFirstRole = (claimValue: string | string[] | undefined): string | undefined => {
            if (claimValue === undefined) return undefined;
            if (Array.isArray(claimValue)) {
                return claimValue.length > 0 ? claimValue[0] : undefined;
            }
            return claimValue;
        };

        userRole = getFirstRole(decoded.role);

        if (!userRole) {
            userRole = getFirstRole(decoded[SPANISH_ROLE_CLAIM]);
        }

        if (!userRole) {
            userRole = getFirstRole(decoded[PLURAL_ROLES_CLAIM]);
        }

        if (!userRole) {
            userRole = getFirstRole(decoded[NET_ROLE_CLAIM]);
        }

        if (!userRole) {
            userRole = getFirstRole(decoded[OLD_NET_ROLE_CLAIM]);
        }

        const expirationTime = decoded.exp * 1000;


        if (Date.now() >= expirationTime) {
            console.warn("Token expirado.");
            logout();
            return <Navigate to="/login" replace />;
        }

        if (!userRole || typeof userRole !== 'string') {

            const payloadJson = JSON.stringify(decoded, null, 2);
            console.error(
                `[FATAL] ROL FALTANTE. El token decodificado NO contiene ninguna de las claves de rol esperadas (role, rol, roles, o claves de .NET). Payload completo: \n${payloadJson}`
            );

            logout();
            return <Navigate to="/login" replace />;
        }

        if (!allowedRoles.includes(userRole)) {
            console.warn(`Acceso denegado: El rol '${userRole}' no tiene permiso para esta ruta.`);

            return <Navigate to="/profile" replace />;
        }

        console.log(`Éxito: Rol '${userRole}' detectado correctamente. Acceso concedido.`);

    } catch (error) {
        console.error("Error al decodificar/validar el token:", error);
        logout();
        return <Navigate to="/login" replace />;
    }

    return <>{children}</>;
};

export default ProtectedRoute;