import React, { useEffect, useState } from 'react';
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
    const [error, setError] = useState<string | null>(null);
    const [msg, setMsg] = useState<string | null>(null);

    const fetchRoles = async () => {
        setLoading(true);
        try {
            const data = await authenticatedFetch<Role[]>('/api/admin/roles', 'GET');
            setRoles(data);
        } catch (err: unknown) { // ⬅️ CORRECCIÓN
            const message = err instanceof Error ? err.message : 'Error desconocido al cargar roles.';
            setError(message);
        } finally {
            setLoading(false);
        }
    };

    const handleCreate = async () => {
        if (!newRole) return;
        try {
            await authenticatedFetch('/api/admin/roles', 'POST', { roleName: newRole });
            setMsg("Rol creado con éxito");
            setNewRole('');
            fetchRoles();
        } catch (err: unknown) { // ⬅️ CORRECCIÓN
            const message = err instanceof Error ? err.message : 'Error al crear rol.';
            setError(message);
        }
    };

    useEffect(() => {
        fetchRoles();
    }, []);

    if (loading) return <Spinner animation="border" />;

    return (
        <div>
            <h4>Gestión de Roles</h4>
            {error && <Alert variant="danger" onClose={() => setError(null)} dismissible>{error}</Alert>}
            {msg && <Alert variant="success" onClose={() => setMsg(null)} dismissible>{msg}</Alert>}

            <InputGroup className="mb-3">
                <Form.Control
                    placeholder="Nombre del nuevo rol"
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
                    </tr>
                </thead>
                <tbody>
                    {roles.map(role => (
                        <tr key={role.id}>
                            <td>{role.name}</td>
                            <td><small className="text-muted">{role.id}</small></td>
                        </tr>
                    ))}
                </tbody>
            </Table>
        </div>
    );
}