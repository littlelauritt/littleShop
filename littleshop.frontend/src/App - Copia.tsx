import { useState } from 'react';
import type { FormEvent } from 'react';
import 'bootstrap/dist/css/bootstrap.min.css';
import './App.css';

const IDENTITY_API_BASE_URL = import.meta.env.VITE_IDENTITY_API_URL;
console.log(IDENTITY_API_BASE_URL);
function App() {
    const [email, setEmail] = useState('');
    const [password, setPassword] = useState('');
    const [message, setMessage] = useState('');
    const [isLoading, setIsLoading] = useState(false);

    const handleRegister = async (e: FormEvent) => {
        e.preventDefault();
        setMessage('');
        setIsLoading(true);

        try {
            const response = await fetch(`${IDENTITY_API_BASE_URL}/api/account/register`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ email, password }),
            });

            setIsLoading(false);

            if (response.ok) {
                setMessage('¡Registro exitoso!');
                setEmail('');
                setPassword('');
            } else {
                const errorData = await response.json();
                let errorMessage = 'Error desconocido.';
                if (Array.isArray(errorData)) {
                    errorMessage = errorData.map((err: { code: string, description: string }) => err.description).join('; ');
                } else if (errorData.message) {
                    errorMessage = errorData.message;
                }
                setMessage(errorMessage);
            }
        } catch {
            setIsLoading(false);
            setMessage('Error de conexión con el servicio de identidad.');
        }
    };

    return (
        <div className="d-flex justify-content-center align-items-center vh-100 bg-dark text-light">
            <div className="card p-4 shadow" style={{ maxWidth: '400px', width: '100%' }}>
                <h2 className="card-title text-center text-primary mb-4">LittleShop | Registro</h2>
                <form onSubmit={handleRegister}>
                    <div className="mb-3">
                        <label htmlFor="email" className="form-label">Email</label>
                        <input
                            id="email"
                            type="email"
                            value={email}
                            onChange={e => setEmail(e.target.value)}
                            className="form-control"
                            placeholder="Introduce tu correo"
                            required
                        />
                    </div>
                    <div className="mb-3">
                        <label htmlFor="password" className="form-label">Contraseña</label>
                        <input
                            id="password"
                            type="password"
                            value={password}
                            onChange={e => setPassword(e.target.value)}
                            className="form-control"
                            placeholder="Mín. 8 caracteres, mayúscula, número, símbolo"
                            required
                        />
                    </div>
                    <button type="submit" disabled={isLoading} className="btn btn-primary w-100">
                        {isLoading ? 'Registrando...' : 'Registrar Cuenta'}
                    </button>
                </form>

                {message && (
                    <div className={`alert mt-3 ${message.includes('exitoso') ? 'alert-success' : 'alert-danger'}`} role="alert">
                        {message}
                    </div>
                )}

                <p className="text-center text-muted mt-3" style={{ fontSize: '0.8rem' }}>
                    * La contraseña debe contener mayúscula, minúscula, número y carácter especial.
                </p>
            </div>
        </div>
    );
}

export default App;