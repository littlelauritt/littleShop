import { useEffect, useState } from 'react';
import { authenticatedFetch } from '../../assets/api';
import { Alert, Spinner, Table, Button, Badge, Pagination } from 'react-bootstrap';

interface User {
    id: string;
    email: string;
    isLocked: boolean;
}

interface PagedUserResponse {
    items?: User[];
    Items?: User[];
    totalPages?: number;
    TotalPages?: number;
}

export default function AdminUserManagement() {
    const [users, setUsers] = useState<User[]>([]);
    const [loading, setLoading] = useState(true);
    const [msg, setMsg] = useState<{ text: string, type: 'success' | 'danger' } | null>(null);

    const [currentPage, setCurrentPage] = useState(1);
    const [totalPages, setTotalPages] = useState(1);
    const pageSize = 10;

    const fetchUsers = async (page: number) => {
        setLoading(true);
        try {
            // GET paginado
            const data = await authenticatedFetch<User[] | PagedUserResponse>(`/api/admin/users?page=${page}&pageSize=${pageSize}`, 'GET');
            
            let list: User[] = [];
            let total = 1;

            if (Array.isArray(data)) {
                list = data;
                total = 1;
            } else {
                list = data.items || data.Items || [];
                total = data.totalPages || data.TotalPages || 1;
            }

            setUsers(list.sort((a, b) => a.email.localeCompare(b.email)));
            setTotalPages(total);
        } catch (err) {
            setMsg({ text: 'Error al cargar usuarios.', type: 'danger' });
            console.error(err);
        } finally {
            setLoading(false);
        }
    };

    const toggleLock = async (userId: string, isLocked: boolean) => {
        try {
            const action = isLocked ? 'unlock' : 'lock';
            await authenticatedFetch(`/api/admin/users/${userId}/${action}`, 'POST');
            setMsg({ text: `Usuario ${isLocked ? 'desbloqueado' : 'bloqueado'} con éxito.`, type: 'success' });
            fetchUsers(currentPage);
        } catch {
            setMsg({ text: 'Error al cambiar el estado.', type: 'danger' });
        }
    };

    const deleteUser = async (userId: string) => {
        if (!confirm('⚠️ ¿Estás SEGURO de eliminar este usuario?')) return;
        try {
            await authenticatedFetch(`/api/admin/users/${userId}`, 'DELETE');
            setMsg({ text: 'Usuario eliminado.', type: 'success' });
            fetchUsers(currentPage);
        } catch {
            setMsg({ text: 'Error al eliminar usuario.', type: 'danger' });
        }
    };

    useEffect(() => { fetchUsers(currentPage); }, [currentPage]);

    if (loading && users.length === 0) return <div className="text-center p-4"><Spinner animation="border" /></div>;

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
                    {users.length === 0 ? <tr><td colSpan={3} className="text-center">No hay usuarios.</td></tr> :
                    users.map(user => (
                        <tr key={user.id}>
                            <td>{user.email}</td>
                            <td>
                                {user.isLocked ? <Badge bg="danger">Bloqueado</Badge> : <Badge bg="success">Activo</Badge>}
                            </td>
                            <td>
                                <div className="d-flex gap-2">
                                    <Button size="sm" variant={user.isLocked ? "success" : "warning"} onClick={() => toggleLock(user.id, user.isLocked)}>
                                        {user.isLocked ? "Desbloquear" : "Bloquear"}
                                    </Button>
                                    <Button size="sm" variant="danger" onClick={() => deleteUser(user.id)}>
                                        Eliminar
                                    </Button>
                                </div>
                            </td>
                        </tr>
                    ))}
                </tbody>
            </Table>

            {totalPages > 1 && (
                <div className="d-flex justify-content-center mt-3">
                    <Pagination>
                        <Pagination.Prev onClick={() => setCurrentPage(p => Math.max(1, p - 1))} disabled={currentPage === 1} />
                        <Pagination.Item active>{currentPage} / {totalPages}</Pagination.Item>
                        <Pagination.Next onClick={() => setCurrentPage(p => Math.min(totalPages, p + 1))} disabled={currentPage === totalPages} />
                    </Pagination>
                </div>
            )}
        </div>
    );
}