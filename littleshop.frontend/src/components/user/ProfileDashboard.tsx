import React from 'react';
import { Container, Tabs, Tab } from 'react-bootstrap';
// Importamos los componentes hijos
import ProfileInfo from './ProfileInfo';
import ChangePassword from './ChangePassword';
import MyOrders from './MyOrders';

export default function ProfileDashboard() {
    return (
        <Container className="mt-4">
            <h1>Mi Perfil 👤</h1>
            {/* IMPORTANTE: <Tabs> debe envolver a todos los <Tab> */}
            <Tabs defaultActiveKey="orders" id="profile-tabs" className="mb-3">

                <Tab eventKey="orders" title="Mis Pedidos">
                    <MyOrders />
                </Tab>

                <Tab eventKey="info" title="Información General">
                    <ProfileInfo />
                </Tab>

                <Tab eventKey="password" title="Cambiar Contraseña">
                    <ChangePassword />
                </Tab>

            </Tabs>
            {/* Fin de Tabs. Si esto falta, da el error que te ha salido */}
        </Container>
    );
}