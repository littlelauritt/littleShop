import { getToken, logout } from "./utils/auth"

// URL del Gateway (inyectada por Aspire o variable de entorno)
const GATEWAY_URL = import.meta.env.VITE_GATEWAY_URL;

// --- TIPOS ---

export interface LoginRequest {
    email: string;
    password: string;
}

export interface RegisterRequest {
    email: string;
    password: string;
}

// --- FUNCIONES DE AUTH ---

export async function loginUser(data: LoginRequest): Promise<string> {
    const response = await fetch(`${GATEWAY_URL}/api/Account/login`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(data),
    });

    if (!response.ok) {
        let message = 'Credenciales incorrectas.';
        try {
            const errorData = await response.json();
            if (errorData.message) message = errorData.message;
            else if (errorData.title) message = errorData.title;
        } catch {
             // Ignorar error de parseo
        }
        throw new Error(message);
    }

    const authResponse = await response.json();

    if (typeof authResponse.token !== 'string') {
        throw new Error('Formato de respuesta de login inválido.');
    }

    return authResponse.token;
}

export async function registerUser(data: RegisterRequest): Promise<void> {
    const response = await fetch(`${GATEWAY_URL}/api/Account/register`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(data),
    });

    if (!response.ok) {
        let message = 'Error en el registro.';
        try {
            const errorData = await response.json();
            message = errorData.message || errorData.title || message;
        } catch {
             // Ignorar error de parseo
        }
        throw new Error(message);
    }
}

// --- FUNCIÓN DE VERIFICACIÓN (Adaptada a tu AccountController.cs) ---
export async function verifyUser(userId: string, code: string): Promise<void> {
    
    // 1. La URL exacta según tu controlador: [HttpPost("confirm-email")]
    const url = `${GATEWAY_URL}/api/Account/confirm-email`;

    console.log(`🔗 Enviando POST a: ${url}`);

    // 2. Enviamos POST con los datos en el BODY (porque usas [FromBody])
    const response = await fetch(url, {
        method: 'POST', 
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ userId, code }) 
    });

    if (!response.ok) {
        let message = 'No se pudo verificar el correo.';
        try {
            const errorData = await response.json();
            // Tu backend devuelve { Message: "..." } o string plano
            message = errorData.Message || errorData.title || message;
        } catch {
             try {
                const textError = await response.text();
                if (textError) message = textError;
             } catch {
                // Comentario para evitar bloque vacío (ESLint)
             }
        }
        console.error(`Error API (${response.status}): ${message}`);
        throw new Error(message);
    }
}

export async function authenticatedFetch<T>(
    path: string,
    method: string = 'GET',
    body: object | null = null
): Promise<T> {
    const token = getToken();

    const headers: HeadersInit = {
        'Content-Type': 'application/json',
    };

    if (token) {
        headers['Authorization'] = `Bearer ${token}`;
    }

    const config: RequestInit = {
        method,
        headers,
    };

    if (body) {
        config.body = JSON.stringify(body);
    }

    const response = await fetch(`${GATEWAY_URL}${path}`, config);

    if (!response.ok) {
        if (response.status === 401 || response.status === 403) {
            if (token) {
                logout();
                window.location.href = '/login';
            }
            throw new Error(`Acceso denegado: ${response.status === 403 ? 'No tienes permiso.' : 'Token inválido.'}`);
        }

        const errorData = await response.json().catch(() => ({ message: `Error ${response.status}: ${response.statusText}` }));
        throw new Error(errorData.message || errorData.title || `Error ${response.status}`);
    }

    return response.status !== 204 ? (await response.json() as T) : {} as T;
}