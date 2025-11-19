import "bootstrap/dist/css/bootstrap.min.css";
import "./App.css";
import { useState } from "react";
import { Routes, Route, Navigate, useNavigate } from "react-router-dom";
import { jwtDecode } from "jwt-decode";

// Componentes
import LoginForm from "./components/LoginForm";
import RegisterForm from "./components/RegisterForm";
import ProfileDashboard from "./components/user/ProfileDashboard";
import AdminDashboard from "./components/admin/AdminDashboard";
import Header from "./components/common/Header";
import ProtectedRoute from './components/common/ProtectedRoute';
import { getToken } from "./assets/utils/auth";

// Tipado para leer el rol al hacer login
interface DecodedToken {
    role?: string;
    "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"?: string;
    // CORRECCIÓN: Usamos 'unknown' en lugar de 'any' para mayor seguridad
    [key: string]: unknown;
}

function AuthPageWrapper() {
    const navigate = useNavigate();
    const isLoginPage = window.location.pathname === '/login';
    const [activeTab, setActiveTab] = useState<'register' | 'login'>(isLoginPage ? 'login' : 'register');

    const onLoginSuccess = () => {
        // Lógica inteligente: Si es Admin -> /admin, si es User -> /profile
        const token = getToken();
        if (token) {
            try {
                const decoded = jwtDecode<DecodedToken>(token);
                // Buscamos el rol en las dos claves posibles
                // CORRECCIÓN: Aseguramos que el valor sea string antes de usarlo
                const role = (decoded.role as string) || (decoded["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] as string);

                if (role === 'Admin') {
                    navigate('/admin');
                } else {
                    navigate('/profile');
                }
            } catch {
                navigate('/profile');
            }
        }
    };

    const handleTabChange = (tab: 'register' | 'login') => {
        setActiveTab(tab);
        navigate(tab === 'login' ? '/login' : '/register', { replace: true });
    };

    return (
        <div className="d-flex justify-content-center align-items-center" style={{ minHeight: "80vh" }}>
            <div className="card p-4 shadow" style={{ maxWidth: "400px", width: "100%", backgroundColor: '#f8f9fa' }}>
                <h2 className="card-title text-center text-primary mb-4">Acceso LittleShop</h2>
                <ul className="nav nav-tabs mb-3">
                    <li className="nav-item">
                        <button className={`nav-link ${activeTab === 'login' ? 'active' : ''}`} onClick={() => handleTabChange('login')}>Login</button>
                    </li>
                    <li className="nav-item">
                        <button className={`nav-link ${activeTab === 'register' ? 'active' : ''}`} onClick={() => handleTabChange('register')}>Registro</button>
                    </li>
                </ul>
                {activeTab === "register" ? <RegisterForm /> : <LoginForm onLogin={onLoginSuccess} />}
            </div>
        </div>
    );
}

export default function App() {
    return (
        <>
            <Header />
            <Routes>
                <Route path="/" element={getToken() ? <Navigate to="/profile" /> : <Navigate to="/login" />} />
                <Route path="/login" element={<AuthPageWrapper />} />
                <Route path="/register" element={<AuthPageWrapper />} />

                {/* Ruta USUARIOS: Accesible para Admin y User */}
                <Route path="/profile" element={
                    <ProtectedRoute allowedRoles={['Admin', 'User']}>
                        <ProfileDashboard />
                    </ProtectedRoute>
                } />

                {/* Ruta ADMIN: Accesible SOLO para Admin */}
                <Route path="/admin" element={
                    <ProtectedRoute allowedRoles={['Admin']}>
                        <AdminDashboard />
                    </ProtectedRoute>
                } />

                <Route path="*" element={<h1 className="text-center mt-5">404 | Página no encontrada</h1>} />
            </Routes>
        </>
    );
}