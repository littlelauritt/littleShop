import React, { useEffect, useState } from 'react';
import { authenticatedFetch } from '../../assets/api';
import { Alert, Spinner, Table, Button } from 'react-bootstrap';

interface User {
    id: string;
    email: string;
    isLocked: boolean;
}

export default function AdminUserManagement() {
    const [users, setUsers] = useState<User[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [msg, setMsg] = useState<string | null>(null);

    // Función para cargar usuarios
    const fetchUsers = async () => {
        setLoading(true);
        try {
            const data = await authenticatedFetch<User[]>('/api/admin/users', 'GET');
            setUsers(data);
        } catch (err: unknown) { // ⬅️ CORRECCIÓN: Usar unknown
            const message = err instanceof Error ? err.message : 'Error desconocido al cargar usuarios.';
            setError(message);
        } finally {
            setLoading(false);
        }
    };

    // Función para Bloquear/Desbloquear
    const toggleLock = async (userId: string, isLocked: boolean) => {
        try {
            const action = isLocked ? 'unlock' : 'lock';
            await authenticatedFetch(`/api/admin/users/${userId}/${action}`, 'POST');
            setMsg(`Usuario ${isLocked ? 'desbloqueado' : 'bloqueado'} correctamente.`);
            fetchUsers(); // Recargar la lista
        } catch (err: unknown) { // ⬅️ CORRECCIÓN: Usar unknown
            const message = err instanceof Error ? err.message : 'Error al cambiar estado.';
            setError(message);
        }
    };

    // Cargar al inicio
    useEffect(() => {
        fetchUsers();
    }, []);

    if (loading) return <Spinner animation="border" />;

    return (
        <div>
            <h4>Gestión de Usuarios</h4>
            {error && <Alert variant="danger" onClose={() => setError(null)} dismissible>{error}</Alert>}
            {msg && <Alert variant="success" onClose={() => setMsg(null)} dismissible>{msg}</Alert>}

            <Table striped bordered hover>
                <thead>
                    <tr>
                        <th>Email</th>
                        <th>Estado</th>
                        <th>Acciones</th>
                    </tr>
                </thead>
                <tbody>
                    {users.map(user => (
                        <tr key={user.id}>
                            <td>{user.email}</td>
                            <td>
                                {user.isLocked ?
                                    <span className="badge bg-danger">Bloqueado</span> :
                                    <span className="badge bg-success">Activo</span>
                                }
                            </td>
                            <td>
                                <Button
                                    size="sm"
                                    variant={user.isLocked ? "success" : "warning"}
                                    onClick={() => toggleLock(user.id, user.isLocked)}
                                >
                                    {user.isLocked ? "Desbloquear" : "Bloquear"}
                                </Button>
                            </td>
                        </tr>
                    ))}
                </tbody>
            </Table>
        </div>
    );
}