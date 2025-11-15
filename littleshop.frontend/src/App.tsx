import { useState } from 'react';
import type { FormEvent } from 'react';
import 'bootstrap/dist/css/bootstrap.min.css';
import './App.css';

const IDENTITY_API_BASE_URL = import.meta.env.VITE_IDENTITY_API_URL;

function App() {
    const [activeTab, setActiveTab] = useState<'register' | 'login'>('register');

    // Estados de registro
    const [regEmail, setRegEmail] = useState('');
    const [regPassword, setRegPassword] = useState('');
    const [regMessage, setRegMessage] = useState('');
    const [regLoading, setRegLoading] = useState(false);

    // Estados de login
    const [loginEmail, setLoginEmail] = useState('');
    const [loginPassword, setLoginPassword] = useState('');
    const [loginMessage, setLoginMessage] = useState('');
    const [loginLoading, setLoginLoading] = useState(false);

    const passwordValidations = [
        { label: 'Mínimo 8 caracteres', test: (pwd: string) => pwd.length >= 8 },
        { label: 'Al menos una mayúscula', test: (pwd: string) => /[A-Z]/.test(pwd) },
        { label: 'Al menos una minúscula', test: (pwd: string) => /[a-z]/.test(pwd) },
        { label: 'Al menos un número', test: (pwd: string) => /\d/.test(pwd) },
        { label: 'Al menos un símbolo', test: (pwd: string) => /[^A-Za-z0-9]/.test(pwd) },
    ];

    const handleRegister = async (e: FormEvent) => {
        e.preventDefault();
        setRegMessage('');
        setRegLoading(true);

        try {
            const response = await fetch(`${IDENTITY_API_BASE_URL}/api/account/register`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ email: regEmail, password: regPassword }),
            });

            setRegLoading(false);

            if (response.ok) {
                setRegMessage('¡Registro exitoso!');
                setRegEmail('');
                setRegPassword('');
            } else {
                const errorData = await response.json();
                let errorMessage = 'Error desconocido.';
                if (Array.isArray(errorData)) {
                    errorMessage = errorData.map((err: { description: string }) => err.description).join('; ');
                } else if (errorData.message) {
                    errorMessage = errorData.message;
                }
                setRegMessage(errorMessage);
            }
        } catch {
            setRegLoading(false);
            setRegMessage('Error de conexión con el servicio de identidad.');
        }
    };

    const handleLogin = async (e: FormEvent) => {
        e.preventDefault();
        setLoginMessage('');
        setLoginLoading(true);

        try {
            const response = await fetch(`${IDENTITY_API_BASE_URL}/api/account/login`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ email: loginEmail, password: loginPassword }),
            });

            setLoginLoading(false);

            if (response.ok) {
                const data = await response.json();
                setLoginMessage('¡Login exitoso!');
                console.log('JWT:', data.token); // Guarda el token donde necesites
                setLoginEmail('');
                setLoginPassword('');
            } else {
                const errorData = await response.json();
                setLoginMessage(errorData.message || 'Error en login.');
            }
        } catch {
            setLoginLoading(false);
            setLoginMessage('Error de conexión con el servicio de identidad.');
        }
    };

    return (
        <div className="d-flex justify-content-center align-items-center vh-100 bg-dark text-light">
            <div className="card p-4 shadow" style={{ maxWidth: '400px', width: '100%' }}>
                <h2 className="card-title text-center text-primary mb-4">LittleShop</h2>

                {/* Tabs */}
                <ul className="nav nav-tabs mb-3">
                    <li className="nav-item">
                        <button
                            className={`nav-link ${activeTab === 'register' ? 'active' : ''}`}
                            onClick={() => setActiveTab('register')}
                        >
                            Registro
                        </button>
                    </li>
                    <li className="nav-item">
                        <button
                            className={`nav-link ${activeTab === 'login' ? 'active' : ''}`}
                            onClick={() => setActiveTab('login')}
                        >
                            Login
                        </button>
                    </li>
                </ul>

                {/* Registro */}
                {activeTab === 'register' && (
                    <form onSubmit={handleRegister}>
                        <div className="mb-3">
                            <label htmlFor="regEmail" className="form-label">Email</label>
                            <input
                                id="regEmail"
                                type="email"
                                value={regEmail}
                                onChange={e => setRegEmail(e.target.value)}
                                className="form-control"
                                placeholder="Introduce tu correo"
                                required
                            />
                        </div>
                        <div className="mb-3">
                            <label htmlFor="regPassword" className="form-label">Contraseña</label>
                            <input
                                id="regPassword"
                                type="password"
                                value={regPassword}
                                onChange={e => setRegPassword(e.target.value)}
                                className="form-control"
                                placeholder="Mín. 8 caracteres, mayúscula, número, símbolo"
                                required
                            />
                            <ul className="password-requirements mt-2 mb-0" style={{ fontSize: '0.85rem' }}>
                                {passwordValidations.map((v, idx) => (
                                    <li key={idx} style={{ color: v.test(regPassword) ? 'green' : 'red' }}>
                                        {v.label}
                                    </li>
                                ))}
                            </ul>
                        </div>
                        <button type="submit" disabled={regLoading} className="btn btn-primary w-100">
                            {regLoading ? 'Registrando...' : 'Registrar Cuenta'}
                        </button>
                        {regMessage && (
                            <div className={`alert mt-3 ${regMessage.includes('exitoso') ? 'alert-success' : 'alert-danger'}`} role="alert">
                                {regMessage}
                            </div>
                        )}
                    </form>
                )}

                {/* Login */}
                {activeTab === 'login' && (
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
                    </form>
                )}

                <p className="text-center text-muted mt-3" style={{ fontSize: '0.8rem' }}>
                    * La contraseña debe contener mayúscula, minúscula, número y carácter especial.
                </p>
            </div>
        </div>
    );
}

export default App;