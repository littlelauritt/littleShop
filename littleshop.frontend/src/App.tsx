import React, { Suspense } from 'react'; // Importamos Suspense
import { Routes, Route, useNavigate } from 'react-router-dom';
import Header from './components/common/Header';
import ProtectedRoute from './components/common/ProtectedRoute';
import { Container, Spinner } from 'react-bootstrap';

// --- LAZY LOADING ---
// En lugar de "import Home from ...", usamos React.lazy.
// Esto le dice a React: "No descargues este código hasta que alguien intente renderizarlo".
const Home = React.lazy(() => import('./components/Home'));
const LoginForm = React.lazy(() => import('./components/LoginForm'));
const RegisterForm = React.lazy(() => import('./components/RegisterForm'));
const AdminDashboard = React.lazy(() => import('./components/admin/AdminDashboard'));
const ProfileDashboard = React.lazy(() => import('./components/user/ProfileDashboard'));
const CartPage = React.lazy(() => import('./components/CartPage'));
const VerifyEmail = React.lazy(() => import('./components/VerifyEmail'));

// Componente visual que se muestra MIENTRAS se descarga el trozo de página nueva
const LoadingFallback = () => (
    <div className="d-flex justify-content-center align-items-center" style={{ minHeight: '60vh' }}>
        <div className="text-center">
            <Spinner animation="border" role="status" variant="primary" style={{ width: '3rem', height: '3rem' }}>
                <span className="visually-hidden">Cargando...</span>
            </Spinner>
            <p className="mt-3 text-muted">Cargando contenido...</p>
        </div>
    </div>
);

function App() {
    const navigate = useNavigate();

    return (
        <>
            {/* El Header se carga siempre (no es lazy) porque sale en todas partes */}
            <Header />
            
            <Container className="py-4">
                {/* Suspense atrapa el momento en que React está descargando el JS de la nueva página */}
                <Suspense fallback={<LoadingFallback />}>
                    <Routes>
                        {/* RUTA PÚBLICA: HOME */}
                        <Route path="/" element={<Home />} />
                        
                        {/* CARRITO Y VERIFICACIÓN */}
                        <Route path="/cart" element={<CartPage />} />
                        <Route path="/verify-email" element={<VerifyEmail />} />
                        
                        {/* LOGIN */}
                        <Route path="/login" element={
                            <div className="row justify-content-center">
                                <div className="col-md-6 col-lg-4">
                                    <h2 className="text-center mb-4">Iniciar Sesión</h2>
                                    <LoginForm onLogin={() => navigate('/profile')} />
                                </div>
                            </div>
                        } />

                        {/* REGISTRO */}
                        <Route path="/register" element={
                            <div className="row justify-content-center">
                                <div className="col-md-6 col-lg-4">
                                    <h2 className="text-center mb-4">Registro</h2>
                                    <RegisterForm />
                                </div>
                            </div>
                        } />

                        {/* RUTA PROTEGIDA: PERFIL */}
                        <Route
                            path="/profile"
                            element={
                                <ProtectedRoute allowedRoles={['Admin', 'User']}>
                                    <ProfileDashboard />
                                </ProtectedRoute>
                            }
                        />

                        {/* RUTA PROTEGIDA: ADMIN */}
                        <Route
                            path="/admin"
                            element={
                                <ProtectedRoute allowedRoles={['Admin']}>
                                    <AdminDashboard />
                                </ProtectedRoute>
                            }
                        />

                        {/* 404 */}
                        <Route path="*" element={<h2 className="text-center mt-5">404 - Página no encontrada 😕</h2>} />

                    </Routes>
                </Suspense>
            </Container>
        </>
    );
}

export default App;