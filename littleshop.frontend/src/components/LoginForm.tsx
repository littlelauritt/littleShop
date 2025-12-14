import { useState } from "react";
import type { FormEvent } from "react";
import { loginUser } from "../assets/api";
import { saveToken, logout, getUserRole } from "../assets/utils/auth"; 
import { useNavigate } from "react-router-dom";
import { Card, Form, Button, Alert, Spinner } from 'react-bootstrap';

interface LoginFormProps {
    onLogin?: () => void;
}

export default function LoginForm({ onLogin }: LoginFormProps) {
    const [loginEmail, setLoginEmail] = useState('');
    const [loginPassword, setLoginPassword] = useState('');
    const [loginMessage, setLoginMessage] = useState('');
    const [loginLoading, setLoginLoading] = useState(false);
    const navigate = useNavigate();

    const handleLogin = async (e: FormEvent) => {
        e.preventDefault();
        setLoginMessage('');
        setLoginLoading(true);

        logout(); 

        try {
            const token = await loginUser({ email: loginEmail, password: loginPassword });
            saveToken(token); 

            setLoginMessage('¡Login exitoso! Redirigiendo...');

            const role = getUserRole(); 

            setTimeout(() => {
                setLoginLoading(false);
                if (role === 'Admin') {
                    navigate('/admin'); 
                } else {
                    navigate('/profile'); 
                }

                if (onLogin) onLogin();
            }, 500); 

        } catch (error) {
            setLoginLoading(false);
            setLoginMessage(`Error: ${error instanceof Error ? error.message : 'Credenciales incorrectas'}`);
        }
    };

    return (
        <Card className="shadow-sm border-0 p-4 mx-auto" style={{ maxWidth: '450px', borderRadius: '20px' }}>
            <Card.Body>
                <h2 className="text-center mb-4" style={{ color: '#4CC9F0' }}>¡Bienvenido!</h2>

                <Form onSubmit={handleLogin}>
                    <Form.Group className="mb-3">
                        <Form.Label className="text-muted small">Email</Form.Label>
                        <Form.Control
                            type="email"
                            placeholder="Introduce tu correo"
                            value={loginEmail}
                            onChange={e => setLoginEmail(e.target.value)}
                            required
                        />
                    </Form.Group>

                    <Form.Group className="mb-4">
                        <Form.Label className="text-muted small">Contraseña</Form.Label>
                        <Form.Control
                            type="password"
                            placeholder="Introduce tu contraseña"
                            value={loginPassword}
                            onChange={e => setLoginPassword(e.target.value)}
                            required
                        />
                    </Form.Group>

                    <Button variant="primary" type="submit" className="w-100 mb-3 py-2" disabled={loginLoading}>
                        {loginLoading ? <Spinner animation="border" size="sm" /> : 'Iniciar Sesión'}
                    </Button>

                    {loginMessage && (
                        <Alert variant={loginMessage.includes('exitoso') ? 'success' : 'danger'} className="py-2 small text-center">
                            {loginMessage}
                        </Alert>
                    )}

                    <div className="text-center mt-3">
                        <Button variant="link" className="text-decoration-none text-muted small" onClick={() => navigate('/register')}>
                            ¿No tienes cuenta? Regístrate aquí
                        </Button>
                    </div>
                </Form>
            </Card.Body>
        </Card>
    );
}