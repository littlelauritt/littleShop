import React from 'react';
import { Container, Tabs, Tab } from 'react-bootstrap';
import AdminUserManagement from './AdminUserManagement';
import AdminRoleManagement from './AdminRoleManagement';
import AdminOrderManagement from './AdminOrderManagement';
import AdminProductManagement from './AdminProductManagement';

export default function AdminDashboard() {
    return (
        <Container className="mt-4">
            <h1>Panel de Administración ⚙️</h1>

            <Tabs defaultActiveKey="products" id="admin-tabs" className="mb-3">

                <Tab eventKey="products" title="📦 Productos">
                    <AdminProductManagement />
                </Tab>

                <Tab eventKey="orders" title="🛒 Pedidos">
                    <AdminOrderManagement />
                </Tab>

                <Tab eventKey="users" title="👥 Usuarios">
                    <AdminUserManagement />
                </Tab>

                <Tab eventKey="roles" title="🛡️ Roles">
                    <AdminRoleManagement />
                </Tab>

            </Tabs>
        </Container>
    );
}