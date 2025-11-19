import React, { useState, useEffect } from 'react';
import { Form, Button, Alert, Card, Spinner } from 'react-bootstrap';
import { authenticatedFetch } from '../../assets/api'; // Corregido: Importamos de api.ts
import { getToken } from '../../assets/utils/auth';

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

    useEffect(() => {
        fetchProfile();
    }, []);

    const fetchProfile = async () => {
        if (!getToken()) {
            setLoading(false);
            return;
        }
        try {
            // Nota: Usamos el genérico <ProfileData> en el fetch
            const data: ProfileData = await authenticatedFetch<ProfileData>('/api/profile/me');
            setProfile(data);
            setNewEmail(data.email);
            setLoading(false);
        } catch (error) {
            console.error(error);
            setStatus({ message: (error as Error).message || 'Error al cargar el perfil.', type: 'danger' });
            setLoading(false);
        }
    };

    const handleUpdate = async (e: React.FormEvent) => {
        e.preventDefault();
        setStatus({ message: '', type: '' });

        try {
            await authenticatedFetch('/api/profile/me', 'PUT', { email: newEmail });
            setStatus({ message: 'Perfil actualizado con éxito.', type: 'success' });
            fetchProfile(); // Recargar datos
        } catch (error) {
            setStatus({ message: (error as Error).message || 'Error al actualizar el perfil.', type: 'danger' });
        }
    };

    if (loading) {
        return <div className='text-center p-5'><Spinner animation="border" /></div>;
    }

    if (!profile) {
        return <Alert variant="danger">No se pudo cargar la información del perfil. ¿Token Admin?</Alert>;
    }

    return (
        <Card className="p-4">
            {status.message && <Alert variant={status.type}>{status.message}</Alert>}
            <Form onSubmit={handleUpdate}>
                <Form.Group className="mb-3">
                    <Form.Label>ID de Usuario</Form.Label>
                    <Form.Control type="text" readOnly value={profile.id} />
                </Form.Group>

                <Form.Group className="mb-3">
                    <Form.Label>Roles</Form.Label>
                    <Form.Control type="text" readOnly value={profile.roles.join(', ')} />
                </Form.Group>

                <Form.Group className="mb-3">
                    <Form.Label>Email</Form.Label>
                    <Form.Control
                        type="email"
                        value={newEmail}
                        onChange={(e) => setNewEmail(e.target.value)}
                        required
                    />
                </Form.Group>
                <Button variant="primary" type="submit">
                    Guardar Cambios
                </Button>
            </Form>
        </Card>
    );
}