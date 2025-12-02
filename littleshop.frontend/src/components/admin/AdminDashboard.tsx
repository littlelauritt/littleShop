import React from 'react';
import { Container, Tabs, Tab } from 'react-bootstrap';
// Importamos los componentes hijos
import AdminUserManagement from './AdminUserManagement';
import AdminRoleManagement from './AdminRoleManagement';
import AdminOrderManagement from './AdminOrderManagement';
import ProfileDashboard from '../user/ProfileDashboard';

export default function AdminDashboard() {
    return (
        <Container className="mt-4">
            <h1>Panel de Administración ⚙️</h1>

            {/* INICIO DE TABS: Debe envolver a TODOS los <Tab> */}
            <Tabs defaultActiveKey="orders" id="admin-tabs" className="mb-3">

                <Tab eventKey="orders" title="Gestión de Pedidos">
                    <AdminOrderManagement />
                </Tab>

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
            {/* FIN DE TABS: Asegúrate de que esta etiqueta de cierre esté aquí */}

        </Container>
    );
}