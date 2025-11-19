// src/assets/api.ts

import { getToken, logout } from "./utils/auth"

const IDENTITY_API_BASE_URL = import.meta.env.VITE_IDENTITY_API_URL;

// --- TIPOS ---

export interface LoginRequest {
    email: string;
    password: string;
}

export interface RegisterRequest {
    email: string;
    password: string;
}

// --- FUNCIONES DE PETICIÓN ---

/**
 * Función para peticiones a la API que NO requieren autenticación (Login y Register).
 * @returns El token JWT como string.
 */
export async function loginUser(data: LoginRequest): Promise<string> {
    const response = await fetch(`${IDENTITY_API_BASE_URL}/api/account/login`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(data),
    });

    if (!response.ok) {
        let message = 'Credenciales incorrectas.';
        try {
            const errorData = await response.json();
            if (errorData.message) {
                message = errorData.message;
            } else if (errorData.title) {
                message = errorData.title;
            }
        } catch {
            // Silenciar el error si response.json() falla.
        }
        throw new Error(message);
    }

    // Tu API devuelve el token (string) directamente.
    const authResponse = await response.json();

    // Aseguramos que la respuesta tiene el campo Token (TypeScript)
    if (typeof authResponse.token !== 'string') {
        throw new Error('Formato de respuesta de login inválido.');
    }

    // Devolvemos solo el string del JWT
    return authResponse.token;
}

// Función de Registro (similar a loginUser)
export async function registerUser(data: RegisterRequest): Promise<void> {
    const response = await fetch(`${IDENTITY_API_BASE_URL}/api/account/register`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(data),
    });

    if (!response.ok) {
        // ... Lógica de manejo de errores de registro ...
        let message = 'Error en el registro.';
        try {
            const errorData = await response.json();
            message = errorData.message || errorData.title || message;
        } catch { /* */ }
        throw new Error(message);
    }

    // 204 No Content para registro exitoso
}


/**
 * Función centralizada de fetch que INCLUYE el token de autorización.
 */
export async function authenticatedFetch<T>(
    path: string,
    method: string = 'GET',
    body: object | null = null
): Promise<T> {
    const token = getToken(); // ⬅️ Usa la función importada de auth.ts
    if (!token) {
        throw new Error('No hay token de autenticación. Por favor, inicia sesión.');
    }

    const headers: HeadersInit = {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`, // ⬅️ CRÍTICO: Envía el token
    };

    const config: RequestInit = {
        method,
        headers,
    };

    if (body) {
        config.body = JSON.stringify(body);
    }

    const response = await fetch(`${IDENTITY_API_BASE_URL}${path}`, config);

    if (!response.ok) {
        // Redirección de seguridad (401/403)
        if (response.status === 401 || response.status === 403) {
            logout(); // Usa la función importada de auth.ts
            window.location.href = '/login';
            throw new Error(`Acceso denegado: ${response.status === 403 ? 'No tienes permiso.' : 'Token inválido o expirado.'}`);
        }

        // Manejo de otros errores
        const errorData = await response.json().catch(() => ({ message: `Error ${response.status}: ${response.statusText}` }));
        throw new Error(errorData.message || errorData.title || `Error ${response.status}`);
    }

    // Manejo de respuesta vacía (204)
    return response.status !== 204 ? (await response.json() as T) : {} as T;
}