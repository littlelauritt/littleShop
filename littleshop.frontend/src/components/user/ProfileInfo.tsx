import React, { useState, useEffect } from 'react';
import { Form, Button, Alert, Card, Spinner } from 'react-bootstrap';
import { authenticatedFetch } from '../../assets/api';
import { getToken, logout } from '../../assets/utils/auth';
import { useNavigate } from 'react-router-dom';

interface ProfileData {
    id: string;
    email: string;
    roles: string[];
}

export default function ProfileInfo() {
    const [profile, setProfile] = useState<ProfileData | null>(null);
    const [newEmail, setNewEmail] = useState('');
    const [loading, setLoading] = useState(true);
    const [status, setStatus] = useState<{ message: string; type: 'success' | 'danger' | '' }>({ message: '', type: '' });

    const navigate = useNavigate();

    useEffect(() => {
        fetchProfile();
    }, []);

    const fetchProfile = async () => {
        if (!getToken()) {
            setLoading(false);
            return;
        }
        try {
            const data: ProfileData = await authenticatedFetch<ProfileData>('/api/profile/me');
            setProfile(data);
            setNewEmail(data.email);
            setLoading(false);
        } catch {
            setStatus({ message: 'Error al cargar perfil.', type: 'danger' });
            setLoading(false);
        }
    };

    const handleUpdate = async (e: React.FormEvent) => {
        e.preventDefault();
        setStatus({ message: '', type: '' });

        if (newEmail === profile?.email) return;

        try {
            await authenticatedFetch('/api/profile/me', 'PUT', { email: newEmail });
            logout();
            alert("Has cambiado tu email. Por seguridad, debes iniciar sesión de nuevo.");
            navigate('/login');
        } catch (error) {
            setStatus({ message: (error as Error).message || 'Error al actualizar.', type: 'danger' });
        }
    };

    if (loading) return <div className='text-center p-5'><Spinner animation="border" /></div>;
    if (!profile) return <Alert variant="danger">No se pudo cargar el perfil.</Alert>;

    return (
        <Card className="p-4 shadow-sm">
            <h4 className="mb-4">Información de la Cuenta</h4>

            {status.message && <Alert variant={status.type}>{status.message}</Alert>}

            <Form onSubmit={handleUpdate}>
                <Form.Group className="mb-3">
                    <Form.Label>Correo Electrónico</Form.Label>
                    <Form.Control
                        type="email"
                        value={newEmail}
                        onChange={(e) => setNewEmail(e.target.value)}
                        required
                    />
                    <Form.Text className="text-muted">
                        Si cambias tu correo, tendrás que iniciar sesión de nuevo.
                    </Form.Text>
                </Form.Group>

                <Button variant="primary" type="submit" disabled={newEmail === profile.email}>
                    Guardar Cambios
                </Button>
            </Form>
        </Card>
    );
}