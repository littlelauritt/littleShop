import React from 'react';
import { Container, Tabs, Tab } from 'react-bootstrap';
// Importamos los componentes que acabamos de crear
import AdminUserManagement from './AdminUserManagement';
import AdminRoleManagement from './AdminRoleManagement';
import ProfileDashboard from '../user/ProfileDashboard'; // Reutilizamos el perfil

export default function AdminDashboard() {
    return (
        <Container className="mt-4">
            <h1>Panel de Administración ⚙️</h1>
            <Tabs defaultActiveKey="users" id="admin-tabs" className="mb-3">
                <Tab eventKey="users" title="Gestión de Usuarios">
                    <AdminUserManagement />
                </Tab>
                <Tab eventKey="roles" title="Gestión de Roles">
                    <AdminRoleManagement />
                </Tab>
                <Tab eventKey="profile" title="Mi Perfil Admin">
                    <ProfileDashboard />
                </Tab>
            </Tabs>
        </Container>
    );
}