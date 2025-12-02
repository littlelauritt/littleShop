import { jwtDecode } from 'jwt-decode';

const TOKEN_KEY = 'littleShopToken';

// 1. Definimos la estructura que esperamos del Token
interface CustomJwtPayload {
    email?: string;
    role?: string | string[];
    roles?: string[];
    // Claves específicas de Microsoft Identity
    "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"?: string;
    "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"?: string | string[];
    // Permitimos otras propiedades desconocidas
    [key: string]: unknown;
}

export function saveToken(token: string) {
    localStorage.setItem(TOKEN_KEY, token);
}

export function getToken(): string | null {
    return localStorage.getItem(TOKEN_KEY);
}

export function logout() {
    localStorage.removeItem(TOKEN_KEY);
}

export function getUserEmail(): string | null {
    const token = getToken();
    if (!token) return null;
    try {
        // 2. Usamos el genérico <CustomJwtPayload> en lugar de 'any'
        const decoded = jwtDecode<CustomJwtPayload>(token);

        return decoded.email ||
            decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'] ||
            null;
    } catch {
        return null;
    }
}

export function getUserRole(): string | null {
    const token = getToken();
    if (!token) return null;
    try {
        // 3. Usamos el genérico aquí también
        const decoded = jwtDecode<CustomJwtPayload>(token);

        const role =
            decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ||
            decoded['role'] ||
            decoded['roles'];

        if (Array.isArray(role)) return role[0];
        // Aseguramos que devolvemos string o null
        return role as string || null;
    } catch {
        return null;
    }
}