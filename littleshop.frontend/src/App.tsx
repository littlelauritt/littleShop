import { Routes, Route, useNavigate } from 'react-router-dom';
import Header from './components/common/Header';
import LoginForm from './components/LoginForm';
import RegisterForm from './components/RegisterForm';
import ProtectedRoute from './components/common/ProtectedRoute';
import AdminDashboard from './components/admin/AdminDashboard';
import ProfileDashboard from './components/user/ProfileDashboard';
import Home from './components/Home';
import { Container } from 'react-bootstrap';
import CartPage from './components/CartPage';

function App() {
    const navigate = useNavigate();

    return (
        <>
            <Header />
            <Container className="py-4">
                <Routes>
                    {/* RUTA PÚBLICA: HOME */}
                    <Route path="/" element={<Home />} />
                    {/* NUEVA RUTA: CARRITO */}
                    <Route path="/cart" element={<CartPage />} />
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

                    {/* RUTA PROTEGIDA: PERFIL (Cualquier usuario logueado) */}
                    <Route
                        path="/profile"
                        element={
                            // Ajusta los roles según cómo los crees en Identity (ej: "User" o "Admin")
                            <ProtectedRoute allowedRoles={['Admin', 'User']}>
                                <ProfileDashboard />
                            </ProtectedRoute>
                        }
                    />

                    {/* RUTA PROTEGIDA: ADMIN (Solo Admin) */}
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
            </Container>
        </>
    );
}

export default App;