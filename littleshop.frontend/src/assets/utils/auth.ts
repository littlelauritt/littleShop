import { jwtDecode } from 'jwt-decode';

const TOKEN_KEY = 'littleShopToken';

interface CustomJwtPayload {
    email?: string;
    role?: string | string[];
    roles?: string[];   
    "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"?: string;
    "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"?: string | string[]; 
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
        
        const decoded = jwtDecode<CustomJwtPayload>(token);

        const role =
            decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ||
            decoded['role'] ||
            decoded['roles'];

        if (Array.isArray(role)) return role[0];

        return role as string || null;
    } catch {
        return null;
    }
}