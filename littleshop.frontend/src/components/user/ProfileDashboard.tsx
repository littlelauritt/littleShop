import React, { useEffect, useState } from 'react';
import { Container, Tabs, Tab } from 'react-bootstrap';
import ProfileInfo from './ProfileInfo';
import ChangePassword from './ChangePassword';
import MyOrders from './MyOrders';
import { getUserRole } from '../../assets/utils/auth';

export default function ProfileDashboard() {
    const [role, setRole] = useState<string | null>(null);

    useEffect(() => {
        setRole(getUserRole());
    }, []);

    const defaultTab = role === 'Admin' ? 'info' : 'orders';
    const tabsKey = role || 'loading';

    return (
        <Container className="mt-4">
            <div className="d-flex align-items-center gap-2 mb-3">
                <h1 className="mb-0">Gestión de Cuenta</h1>
                {role === 'Admin' && <span className="badge bg-warning text-dark">Modo Admin</span>}
            </div>

            <Tabs defaultActiveKey={defaultTab} id={`profile-tabs-${tabsKey}`} className="mb-3">
                {role !== 'Admin' && (
                    <Tab eventKey="orders" title="📦 Mis Pedidos">
                        <MyOrders />
                    </Tab>
                )}
                <Tab eventKey="info" title="👤 Datos Personales">
                    <ProfileInfo />
                </Tab>
                <Tab eventKey="password" title="🔒 Seguridad">
                    <ChangePassword />
                </Tab>
            </Tabs>
        </Container>
    );
}