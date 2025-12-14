import { useEffect, useState } from 'react';
import { authenticatedFetch } from '../../assets/api';
import { Alert, Spinner, Table, Button, Form, InputGroup, Pagination } from 'react-bootstrap';

interface Role {
    id: string;
    name: string;
}

interface PagedRoleResponse {
    items?: Role[];
    Items?: Role[];
    totalPages?: number;
    TotalPages?: number;
}

export default function AdminRoleManagement() {
    const [roles, setRoles] = useState<Role[]>([]);
    const [newRole, setNewRole] = useState('');
    const [loading, setLoading] = useState(true);
    const [msg, setMsg] = useState<{ text: string, type: 'success' | 'danger' } | null>(null);

    const [currentPage, setCurrentPage] = useState(1);
    const [totalPages, setTotalPages] = useState(1);
    const pageSize = 10;

    const fetchRoles = async (page: number) => {
        setLoading(true);
        try {
            const data = await authenticatedFetch<Role[] | PagedRoleResponse>(`/api/admin/roles?page=${page}&pageSize=${pageSize}`, 'GET');
            
            let list: Role[] = [];
            let total = 1;

            if (Array.isArray(data)) {
                list = data;
                total = 1;
            } else {
                list = data.items || data.Items || [];
                total = data.totalPages || data.TotalPages || 1;
            }
            setRoles(list);
            setTotalPages(total);
        } catch {
            setMsg({ text: 'Error al cargar roles.', type: 'danger' });
        } finally {
            setLoading(false);
        }
    };

    const handleCreate = async () => {
        if (!newRole) return;
        try {
            await authenticatedFetch('/api/admin/roles', 'POST', { roleName: newRole });
            setMsg({ text: "Rol creado con éxito", type: 'success' });
            setNewRole('');
            fetchRoles(currentPage);
        } catch {
            setMsg({ text: 'Error al crear rol.', type: 'danger' });
        }
    };

    const handleDelete = async (id: string) => {
        if (!confirm('¿Borrar rol?')) return;
        try {
            await authenticatedFetch(`/api/admin/roles/${id}`, 'DELETE');
            setMsg({ text: "Rol eliminado", type: 'success' });
            fetchRoles(currentPage);
        } catch {
            setMsg({ text: 'Error al eliminar rol.', type: 'danger' });
        }
    }

    useEffect(() => { fetchRoles(currentPage); }, [currentPage]);

    if (loading && roles.length === 0) return <div className="text-center p-3"><Spinner animation="border" /></div>;

    return (
        <div>
            <h4 className="mb-3">Gestión de Roles</h4>
            {msg && <Alert variant={msg.type} onClose={() => setMsg(null)} dismissible>{msg.text}</Alert>}

            <InputGroup className="mb-3">
                <Form.Control
                    placeholder="Nombre del nuevo rol (ej: Manager)"
                    value={newRole}
                    onChange={(e) => setNewRole(e.target.value)}
                />
                <Button variant="primary" onClick={handleCreate}>Crear Rol</Button>
            </InputGroup>

            <Table hover responsive className="align-middle">
                <thead>
                    <tr>
                        <th>Nombre del Rol</th>
                        <th>ID</th>
                        <th>Acción</th>
                    </tr>
                </thead>
                <tbody>
                    {roles.length === 0 ? <tr><td colSpan={3} className="text-center">No hay roles.</td></tr> :
                    roles.map(role => (
                        <tr key={role.id}>
                            <td>{role.name}</td>
                            <td><small className="text-muted">{role.id}</small></td>
                            <td>
                                {role.name !== 'Admin' && (
                                    <Button variant="outline-danger" size="sm" onClick={() => handleDelete(role.id)}>
                                        Borrar
                                    </Button>
                                )}
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