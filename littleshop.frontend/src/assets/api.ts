import { getToken, logout } from "./utils/auth"

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
        // CORRECCIÓN: Leemos texto primero para no perder el stream
        const text = await response.text();
        let message = 'Credenciales incorrectas.';
        try {
            const errorData = JSON.parse(text);
            if (errorData.message) message = errorData.message;
            else if (errorData.title) message = errorData.title;
        } catch {
            if (text) message = text;
        }
        throw new Error(message);
    }

    const authResponse = await response.json();
    return authResponse.token;
}

export async function registerUser(data: RegisterRequest): Promise<void> {
    const response = await fetch(`${GATEWAY_URL}/api/Account/register`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(data),
    });

    if (!response.ok) {
        const text = await response.text();
        let message = 'Error en el registro.';
        try {
            const errorData = JSON.parse(text);
            message = errorData.message || errorData.title || message;
        } catch {
            if (text) message = text;
        }
        throw new Error(message);
    }
}

// --- FUNCIÓN DE VERIFICACIÓN (ARREGLADA) ---
export async function verifyUser(userId: string, code: string): Promise<void> {
    
    const url = `${GATEWAY_URL}/api/Account/confirm-email`;
    console.log(`🔗 Enviando POST a: ${url}`);

    const response = await fetch(url, {
        method: 'POST', 
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ userId, code }) 
    });

    if (!response.ok) {
        // 1. Leemos el cuerpo como TEXTO primero (esto nunca falla)
        const errorText = await response.text();
        let message = 'No se pudo verificar el correo.';
        
        try {
            // 2. Intentamos parsear a JSON si es posible
            const errorJson = JSON.parse(errorText);
            message = errorJson.Message || errorJson.title || message;
        } catch {
            // 3. Si no es JSON, usamos el texto crudo del servidor (aquí estará el error real)
            if (errorText) message = errorText;
        }

        console.error(`❌ Error API (${response.status}):`, message);
        throw new Error(message);
    }
}

export async function authenticatedFetch<T>(
    path: string,
    method: string = 'GET',
    body: object | null = null
): Promise<T> {
    const token = getToken();
    const headers: HeadersInit = { 'Content-Type': 'application/json' };
    if (token) headers['Authorization'] = `Bearer ${token}`;

    const config: RequestInit = { method, headers };
    if (body) config.body = JSON.stringify(body);

    const response = await fetch(`${GATEWAY_URL}${path}`, config);

    if (!response.ok) {
        if (response.status === 401 || response.status === 403) {
            if (token) { logout(); window.location.href = '/login'; }
            throw new Error(`Acceso denegado: ${response.status === 403 ? 'No tienes permiso.' : 'Token inválido.'}`);
        }

        // CORRECCIÓN: Lectura segura del error
        const text = await response.text();
        let message = `Error ${response.status}`;
        try {
            const json = JSON.parse(text);
            message = json.message || json.title || message;
        } catch {
            if (text) message = text;
        }
        throw new Error(message);
    }

    return response.status !== 204 ? (await response.json() as T) : {} as T;
}