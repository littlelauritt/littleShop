import React, { useState } from 'react';
import { Form, Button, Alert, Card, Spinner } from 'react-bootstrap';
import { authenticatedFetch } from '../../assets/api'; // Corregido: Importamos de api.ts

export default function ChangePassword() {
    const [currentPassword, setCurrentPassword] = useState('');
    const [newPassword, setNewPassword] = useState('');
    const [status, setStatus] = useState<{ message: string; type: 'success' | 'danger' | '' }>({ message: '', type: '' });
    const [loading, setLoading] = useState(false);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setLoading(true);
        setStatus({ message: '', type: '' });

        try {
            await authenticatedFetch('/api/profile/change-password', 'POST', {
                currentPassword,
                newPassword
            });

            setStatus({ message: 'Contraseña actualizada con éxito. ¡Vuelve a logearte si el token expira pronto!', type: 'success' });
            setCurrentPassword('');
            setNewPassword('');
        } catch (error) {
            setStatus({ message: (error as Error).message || 'Error al cambiar la contraseña.', type: 'danger' });
        } finally {
            setLoading(false);
        }
    };

    return (
        <Card className="p-4">
            {status.message && <Alert variant={status.type}>{status.message}</Alert>}
            <Form onSubmit={handleSubmit}>
                <Form.Group className="mb-3">
                    <Form.Label>Contraseña Actual</Form.Label>
                    <Form.Control
                        type="password"
                        value={currentPassword}
                        onChange={(e) => setCurrentPassword(e.target.value)}
                        required
                    />
                </Form.Group>
                <Form.Group className="mb-3">
                    <Form.Label>Nueva Contraseña</Form.Label>
                    <Form.Control
                        type="password"
                        value={newPassword}
                        onChange={(e) => setNewPassword(e.target.value)}
                        required
                    />
                    <Form.Text className="text-muted">
                        Mín. 8 caracteres, mayúscula, minúscula, número y símbolo.
                    </Form.Text>
                </Form.Group>
                <Button variant="danger" type="submit" disabled={loading}>
                    {loading ? <Spinner animation="border" size="sm" /> : 'Cambiar Contraseña'}
                </Button>
            </Form>
        </Card>
    );
}