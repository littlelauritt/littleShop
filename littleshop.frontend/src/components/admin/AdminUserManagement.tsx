import { useEffect, useState } from 'react';
import { authenticatedFetch } from '../../assets/api';
import { Alert, Spinner, Table, Button, Badge } from 'react-bootstrap';

// Definimos la interfaz basándonos en lo que devuelve tu AdminUsersController
interface User {
    id: string;
    email: string;
    isLocked: boolean; // Tu controlador devuelve "isLocked"
}

export default function AdminUserManagement() {
    const [users, setUsers] = useState<User[]>([]);
    const [loading, setLoading] = useState(true);
    const [msg, setMsg] = useState<{ text: string, type: 'success' | 'danger' } | null>(null);

    // 1. CARGAR USUARIOS
    const fetchUsers = async () => {
        setLoading(true);
        try {
            const data = await authenticatedFetch<User[]>('/api/admin/users', 'GET');
            // Ordenamos por email para que sea más fácil buscar
            setUsers(data.sort((a, b) => a.email.localeCompare(b.email)));
        } catch (err) {
            setMsg({ text: 'Error al cargar usuarios. ¿Eres Admin?', type: 'danger' });
            console.error(err);
        } finally {
            setLoading(false);
        }
    };

    // 2. BLOQUEAR / DESBLOQUEAR
    const toggleLock = async (userId: string, isLocked: boolean) => {
        try {
            // Tu API espera /lock o /unlock
            const action = isLocked ? 'unlock' : 'lock';
            await authenticatedFetch(`/api/admin/users/${userId}/${action}`, 'POST');

            setMsg({ text: `Usuario ${isLocked ? 'desbloqueado' : 'bloqueado'} con éxito.`, type: 'success' });
            fetchUsers(); // Recargamos la lista
        } catch {
            setMsg({ text: 'Error al cambiar el estado del usuario.', type: 'danger' });
        }
    };

    // 3. ELIMINAR USUARIO
    const deleteUser = async (userId: string) => {
        if (!confirm('⚠️ ¿Estás SEGURO de eliminar este usuario? Esta acción es irreversible.')) return;

        try {
            await authenticatedFetch(`/api/admin/users/${userId}`, 'DELETE');
            setMsg({ text: 'Usuario eliminado correctamente.', type: 'success' });
            fetchUsers();
        } catch {
            setMsg({ text: 'Error al eliminar usuario.', type: 'danger' });
        }
    };

    useEffect(() => {
        fetchUsers();
    }, []);

    if (loading) return <div className="text-center p-4"><Spinner animation="border" /></div>;

    return (
        <div>
            <h4 className="mb-3">Gestión de Usuarios</h4>
            {msg && <Alert variant={msg.type} onClose={() => setMsg(null)} dismissible>{msg.text}</Alert>}

            <Table striped bordered hover responsive>
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
                                    <Badge bg="danger">Bloqueado</Badge> :
                                    <Badge bg="success">Activo</Badge>
                                }
                            </td>
                            <td>
                                <div className="d-flex gap-2">
                                    <Button
                                        size="sm"
                                        variant={user.isLocked ? "success" : "warning"}
                                        onClick={() => toggleLock(user.id, user.isLocked)}
                                    >
                                        {user.isLocked ? "Desbloquear" : "Bloquear"}
                                    </Button>

                                    <Button
                                        size="sm"
                                        variant="danger"
                                        onClick={() => deleteUser(user.id)}
                                    >
                                        Eliminar
                                    </Button>
                                </div>
                            </td>
                        </tr>
                    ))}
                </tbody>
            </Table>
        </div>
    );
}