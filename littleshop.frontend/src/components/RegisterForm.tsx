import { useState } from "react";
import type { FormEvent } from "react";
import { registerUser } from "../assets/api";
import { Card, Form, Button, Alert, Spinner } from 'react-bootstrap';
import { useNavigate } from "react-router-dom";

export default function RegisterForm() {
    const [regEmail, setRegEmail] = useState('');
    const [regPassword, setRegPassword] = useState('');
    const [regMessage, setRegMessage] = useState('');
    const [regLoading, setRegLoading] = useState(false);
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
            setRegMessage('¡Cuenta creada con éxito! Revisa tu email para confirmarla.');
            setRegEmail('');
            setRegPassword('');
        } catch (error) {
            setRegLoading(false);
            setRegMessage((error as Error).message || 'Error al registrar la cuenta.');
        }
    };

    return (
        <Card className="shadow border-0 p-4 mx-auto" style={{ maxWidth: '450px', borderRadius: '20px' }}>
            <Card.Body>
                <h2 className="text-center mb-4" style={{ color: '#4CC9F0' }}>Crear Cuenta</h2>

                <Form onSubmit={handleRegister}>
                    <Form.Group className="mb-3">
                        <Form.Label className="text-muted small">Correo Electrónico</Form.Label>
                        <Form.Control
                            type="email"
                            placeholder="tu@email.com"
                            value={regEmail}
                            onChange={e => setRegEmail(e.target.value)}
                            required
                        />
                    </Form.Group>

                    <Form.Group className="mb-3">
                        <Form.Label className="text-muted small">Contraseña</Form.Label>
                        <Form.Control
                            type="password"
                            placeholder="Crea una contraseña segura"
                            value={regPassword}
                            onChange={e => setRegPassword(e.target.value)}
                            required
                        />

                        {/* ✅ RECUPERADO: Lista visual de validaciones */}
                        <div className="mt-2 p-2 bg-light rounded border border-0">
                            {passwordValidations.map((v, idx) => {
                                const isValid = v.test(regPassword);
                                return (
                                    <div
                                        key={idx}
                                        style={{
                                            fontSize: '0.75rem',
                                            color: isValid ? '#2ecc71' : '#adb5bd',
                                            marginBottom: '2px',
                                            transition: 'color 0.3s ease'
                                        }}
                                    >
                                        <span style={{ marginRight: '5px' }}>
                                            {isValid ? '✔' : '•'}
                                        </span>
                                        {v.label}
                                    </div>
                                );
                            })}
                        </div>
                    </Form.Group>

                    <Button variant="primary" type="submit" className="w-100 mt-2 py-2" disabled={regLoading}>
                        {regLoading ? <Spinner animation="border" size="sm" /> : 'Registrar Cuenta'}
                    </Button>

                    {regMessage && (
                        <Alert variant={regMessage.includes('creada') ? 'success' : 'danger'} className="mt-3 py-2 small text-center">
                            {regMessage}
                        </Alert>
                    )}

                    <div className="text-center mt-3">
                        <Button variant="link" className="text-decoration-none text-muted small" onClick={() => navigate('/login')}>
                            ¿Ya tienes cuenta? Inicia sesión
                        </Button>
                    </div>
                </Form>
            </Card.Body>
        </Card>
    );
}