// src/components/user/ProfileDashboard.tsx
import React from 'react';
import { Container, Tabs, Tab } from 'react-bootstrap'
import ProfileInfo from './ProfileInfo';
import ChangePassword from './ChangePassword'; 

export default function ProfileDashboard() {
    return (
        <Container className="mt-4">
            <h1>Mi Perfil 👤</h1>
            <Tabs defaultActiveKey="info" id="uncontrolled-tab-example" className="mb-3">
                <Tab eventKey="info" title="Información General">
                    <ProfileInfo />
                </Tab>
                <Tab eventKey="password" title="Cambiar Contraseña">
                    <ChangePassword />                    
                </Tab>
            </Tabs>
        </Container>
    );
}