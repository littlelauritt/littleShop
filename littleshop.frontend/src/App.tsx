import React, { Suspense } from 'react';
import { Routes, Route, useNavigate } from 'react-router-dom';
import Header from './components/common/Header';
import ProtectedRoute from './components/common/ProtectedRoute';
import { Container, Spinner } from 'react-bootstrap';

// Lazy Imports
const Home = React.lazy(() => import('./components/Home'));
const LoginForm = React.lazy(() => import('./components/LoginForm'));
const RegisterForm = React.lazy(() => import('./components/RegisterForm'));
const AdminDashboard = React.lazy(() => import('./components/admin/AdminDashboard'));
const ProfileDashboard = React.lazy(() => import('./components/user/ProfileDashboard'));
const CartPage = React.lazy(() => import('./components/CartPage'));
const VerifyEmail = React.lazy(() => import('./components/VerifyEmail'));

const LoadingFallback = () => (
    <div className="d-flex justify-content-center align-items-center" style={{ minHeight: '60vh' }}>
        <div className="text-center text-primary">
            <Spinner animation="border" role="status" variant="primary" style={{ width: '3rem', height: '3rem' }}>
                <span className="visually-hidden">Cargando...</span>
            </Spinner>
            <p className="mt-3 fw-bold" style={{ color: '#4CC9F0' }}>Cargando...</p>
        </div>
    </div>
);

function App() {
    const navigate = useNavigate();

    return (
        <div style={{ minHeight: '100vh', display: 'flex', flexDirection: 'column' }}>
            <Header />

            <Container className="py-5 flex-grow-1">
                <Suspense fallback={<LoadingFallback />}>
                    <Routes>
                        <Route path="/" element={<Home />} />
                        <Route path="/cart" element={<CartPage />} />
                        <Route path="/verify-email" element={<VerifyEmail />} />

                        {/* ✅ CORREGIDO: Quitamos 'onLogin' para que el componente decida dónde ir */}
                        <Route path="/login" element={
                            <div className="row justify-content-center mt-4">
                                <div className="col-md-6 col-lg-5">
                                    <LoginForm />
                                </div>
                            </div>
                        } />

                        <Route path="/register" element={
                            <div className="row justify-content-center mt-4">
                                <div className="col-md-6 col-lg-5">
                                    <RegisterForm />
                                </div>
                            </div>
                        } />

                        <Route path="/profile" element={
                            <ProtectedRoute allowedRoles={['Admin', 'User']}>
                                <ProfileDashboard />
                            </ProtectedRoute>
                        } />

                        <Route path="/admin" element={
                            <ProtectedRoute allowedRoles={['Admin']}>
                                <AdminDashboard />
                            </ProtectedRoute>
                        } />

                        <Route path="*" element={
                            <div className="text-center mt-5">
                                <h1 style={{ fontSize: '4rem' }}>🙈</h1>
                                <h2 className="text-muted">Ups, página no encontrada</h2>
                                <button className="btn btn-primary mt-3" onClick={() => navigate('/')}>Volver al inicio</button>
                            </div>
                        } />
                    </Routes>
                </Suspense>
            </Container>

            <footer className="py-4 text-center text-muted mt-auto bg-white border-top">
                <small>Made with 💖 by Laura & LittleShop</small>
            </footer>
        </div>
    );
}

export default App;