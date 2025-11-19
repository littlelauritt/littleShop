export interface RegisterRequest {
    email: string;
    password: string;
    confirmPassword?: string;
}

export interface LoginRequest {
    email: string;
    password: string;
}

export interface AuthResponse {
    accessToken: string;
    tokenType: string;
    expiresIn: number;
    userId: string;
    email: string;
    roles: string[];
}
