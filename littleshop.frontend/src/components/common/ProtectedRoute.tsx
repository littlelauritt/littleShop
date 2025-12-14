import React from 'react';
import { Navigate } from 'react-router-dom';
import { jwtDecode } from 'jwt-decode';
import { getToken, logout } from '../../assets/utils/auth';

const MICROSOFT_ROLE_CLAIM = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";

interface JwtPayload {
    exp: number;
    role?: string | string[];
    [MICROSOFT_ROLE_CLAIM]?: string | string[];
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
        const longClaim = decoded[MICROSOFT_ROLE_CLAIM];
        const shortClaim = decoded.role;
        const roleValue = longClaim || shortClaim;

        if (roleValue) {
            if (Array.isArray(roleValue)) {
                const hasMatchingRole = roleValue.some(r => allowedRoles.includes(r));
                if (hasMatchingRole) {
                    userRole = roleValue.find(r => allowedRoles.includes(r));
                } else {
                    userRole = roleValue[0]; 
                }
            } else {
                userRole = roleValue as string;
            }
        }

        const expirationTime = decoded.exp * 1000;

        if (Date.now() >= expirationTime) {
            console.warn("Token expirado.");
            logout();
            return <Navigate to="/login" replace />;
        }

        if (!userRole) {
            console.error("Error Crítico: El token es válido pero no se encontró ningún rol (ni corto ni largo).");
            console.log("Token decodificado:", decoded);
            logout();
            return <Navigate to="/login" replace />;
        }

        if (!allowedRoles.includes(userRole)) {
            console.warn(`Acceso denegado: El rol '${userRole}' no está en la lista permitida [${allowedRoles.join(', ')}].`);
            return <Navigate to="/profile" replace />;
        }

    } catch (error) {
        console.error("Error al procesar el token:", error);
        logout();
        return <Navigate to="/login" replace />;
    }

    return <>{children}</>;
};

export default ProtectedRoute;