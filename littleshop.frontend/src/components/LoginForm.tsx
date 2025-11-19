import { useState } from "react";
import type { FormEvent } from "react";
import { loginUser } from "../assets/api";
import { saveToken, logout } from "../assets/utils/auth";
import { useNavigate } from "react-router-dom"; // Necesitas este hook para navegar

interface LoginFormProps {
    // onLogin: función que se llama en App.tsx para navegar tras el éxito
    onLogin: () => void;
}

export default function LoginForm({ onLogin }: LoginFormProps) {
    const [loginEmail, setLoginEmail] = useState('');
    const [loginPassword, setLoginPassword] = useState('');
    const [loginMessage, setLoginMessage] = useState('');
    const [loginLoading, setLoginLoading] = useState(false);
    const navigate = useNavigate(); // Hook de navegación

    const handleLogin = async (e: FormEvent) => {
        e.preventDefault();
        setLoginMessage('');
        setLoginLoading(true);

        // Opcional: Limpiamos por si hay un token viejo que pueda causar conflictos
        logout();

        try {
            // Usamos la función centralizada de loginUser de api.ts
            const token = await loginUser({ email: loginEmail, password: loginPassword });

            setLoginLoading(false);

            // La función loginUser devuelve el JWT (string) si tiene éxito.
            saveToken(token); // ⬅️ Guardamos el JWT (string) directamente

            setLoginMessage('¡Login exitoso! Redirigiendo...');
            setLoginEmail('');
            setLoginPassword('');

            // Llamamos a la función de prop para que App.tsx o el wrapper maneje la redirección
            onLogin();

        } catch (error) {
            setLoginLoading(false);
            // El error ya viene como un objeto Error de tu función loginUser, si usamos api.ts
            // Si hay error 401, el mensaje vendrá de errorData en api.ts
            setLoginMessage(`Error en login: ${error instanceof Error ? error.message : 'Verifica credenciales.'}`);
        }
    };

    return (
        <form onSubmit={handleLogin}>
            <div className="mb-3">
                <label htmlFor="loginEmail" className="form-label">Email</label>
                <input
                    id="loginEmail"
                    type="email"
                    value={loginEmail}
                    onChange={e => setLoginEmail(e.target.value)}
                    className="form-control"
                    placeholder="Introduce tu correo"
                    required
                />
            </div>
            <div className="mb-3">
                <label htmlFor="loginPassword" className="form-label">Contraseña</label>
                <input
                    id="loginPassword"
                    type="password"
                    value={loginPassword}
                    onChange={e => setLoginPassword(e.target.value)}
                    className="form-control"
                    placeholder="Introduce tu contraseña"
                    required
                />
            </div>
            <button type="submit" disabled={loginLoading} className="btn btn-primary w-100">
                {loginLoading ? 'Iniciando sesión...' : 'Iniciar Sesión'}
            </button>
            {loginMessage && (
                <div className={`alert mt-3 ${loginMessage.includes('exitoso') ? 'alert-success' : 'alert-danger'}`} role="alert">
                    {loginMessage}
                </div>
            )}
            <button
                type="button"
                className="btn btn-link mt-2 w-100"
                onClick={() => navigate('/register')} // Usamos navigate para cambiar a registro
            >
                ¿No tienes cuenta? Regístrate aquí
            </button>
        </form>
    );
}