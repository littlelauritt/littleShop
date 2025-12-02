import { useState } from "react";
import type { FormEvent } from "react";
import { registerUser } from "../assets/api";
import { useNavigate } from "react-router-dom";

export default function RegisterForm() {
    const [regEmail, setRegEmail] = useState('');
    const [regPassword, setRegPassword] = useState('');
    const [regMessage, setRegMessage] = useState('');
    const [regLoading, setRegLoading] = useState(false);

    // 1. Inicializamos el hook
    const navigate = useNavigate();

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
            await registerUser({ email: regEmail, password: regPassword });

            setRegLoading(false);
            setRegMessage('¡Registro exitoso! Redirigiendo al login...');
            setRegEmail('');
            setRegPassword('');

            // 2. Redirigimos al Login tras 2 segundos
            setTimeout(() => {
                navigate('/login');
            }, 2000);

        } catch (error) {
            setRegLoading(false);
            setRegMessage((error as Error).message || 'Error de conexión.');
        }
    };

    return (
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
    );
}