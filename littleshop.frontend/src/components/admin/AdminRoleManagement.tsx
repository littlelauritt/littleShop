import { useEffect, useState } from 'react';
import { authenticatedFetch } from '../../assets/api';
import { Alert, Spinner, Table, Button, Form, InputGroup } from 'react-bootstrap';

interface Role {
    id: string;
    name: string;
}

export default function AdminRoleManagement() {
    const [roles, setRoles] = useState<Role[]>([]);
    const [newRole, setNewRole] = useState('');
    const [loading, setLoading] = useState(true);
    const [msg, setMsg] = useState<{ text: string, type: 'success' | 'danger' } | null>(null);

    const fetchRoles = async () => {
        setLoading(true);
        try {
            // GET /api/admin/roles
            const data = await authenticatedFetch<Role[]>('/api/admin/roles', 'GET');
            setRoles(data);
        } catch {
            setMsg({ text: 'Error al cargar roles.', type: 'danger' });
        } finally {
            setLoading(false);
        }
    };

    const handleCreate = async () => {
        if (!newRole) return;
        try {
            // POST /api/admin/roles
            await authenticatedFetch('/api/admin/roles', 'POST', { roleName: newRole });
            setMsg({ text: "Rol creado con éxito", type: 'success' });
            setNewRole('');
            fetchRoles();
        } catch {
            setMsg({ text: 'Error al crear rol.', type: 'danger' });
        }
    };

    const handleDelete = async (id: string) => {
        if (!confirm('¿Borrar rol?')) return;
        try {
            // DELETE /api/admin/roles/{id}
            await authenticatedFetch(`/api/admin/roles/${id}`, 'DELETE');
            setMsg({ text: "Rol eliminado", type: 'success' });
            fetchRoles();
        } catch {
            setMsg({ text: 'Error al eliminar rol.', type: 'danger' });
        }
    }

    useEffect(() => {
        fetchRoles();
    }, []);

    if (loading) return <div className="text-center p-3"><Spinner animation="border" /></div>;

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

            <Table striped bordered hover>
                <thead>
                    <tr>
                        <th>Nombre del Rol</th>
                        <th>ID</th>
                        <th>Acción</th>
                    </tr>
                </thead>
                <tbody>
                    {roles.map(role => (
                        <tr key={role.id}>
                            <td>{role.name}</td>
                            <td><small className="text-muted">{role.id}</small></td>
                            <td>
                                {/* Evitamos borrar el rol Admin por seguridad visual */}
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
        </div>
    );
}