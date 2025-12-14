import { useNavigate, useLocation } from 'react-router-dom';
import { getToken, logout, getUserRole, getUserEmail } from '../../assets/utils/auth';
import { useState, useEffect } from 'react';
import { useCart } from '../../context/CartContext';
import { Container, Navbar, Nav, Button, Badge } from 'react-bootstrap';

export default function Header() {
    const navigate = useNavigate();
    const location = useLocation();
    const [token, setToken] = useState<string | null>(getToken());
    const [role, setRole] = useState<string | null>(null);
    const [email, setEmail] = useState<string | null>(null);
    const { cartCount } = useCart();

    const handleLogout = () => {
        logout();
        setToken(null);
        setRole(null);
        setEmail(null);
        navigate('/login');
    };

    useEffect(() => {
        setToken(getToken());
        setRole(getUserRole());
        setEmail(getUserEmail());
    }, [location]);

    return (
        <Navbar expand="lg" className="bg-white sticky-top shadow-sm py-3">
            <Container>
                {/* ✅ CAMBIO: Color ROSA (#ff7696) y SIN emojis */}
                <Navbar.Brand
                    href="/"
                    onClick={(e) => { e.preventDefault(); navigate('/'); }}
                    className="fw-bold fs-3"
                    style={{ color: '#ff7696' }}
                >
                    LittleShop
                </Navbar.Brand>

                <Navbar.Toggle aria-controls="navbar-nav" />

                <Navbar.Collapse id="navbar-nav">
                    <Nav className="ms-auto align-items-center gap-3">

                        {/* Botón Carrito (Mantenemos el estilo Cyan clarito que te gustaba) */}
                        <Button
                            variant="light"
                            className="position-relative fw-bold"
                            style={{ backgroundColor: '#e0f7fa', color: '#00B4D8', border: 'none' }}
                            onClick={() => navigate('/cart')}
                        >
                            Carrito
                            {cartCount > 0 && (
                                <Badge bg="danger" pill className="position-absolute top-0 start-100 translate-middle border border-light">
                                    {cartCount}
                                </Badge>
                            )}
                        </Button>

                        {token ? (
                            <>
                                <span className="text-muted d-none d-lg-block small me-2">
                                    Hola, <strong>{email?.split('@')[0]}</strong>
                                </span>

                                <Button
                                    variant="outline-secondary"
                                    size="sm"
                                    onClick={() => navigate('/profile')}
                                >
                                    👤 Mi Perfil
                                </Button>

                                {role === 'Admin' && (
                                    <Button
                                        variant="outline-primary"
                                        size="sm"
                                        onClick={() => navigate('/admin')}
                                    >
                                        ⚙️ Panel Admin
                                    </Button>
                                )}

                                <Button variant="link" className="text-danger text-decoration-none small" onClick={handleLogout}>
                                    Salir
                                </Button>
                            </>
                        ) : (
                            <Button variant="primary" onClick={() => navigate('/login')}>
                                Iniciar Sesión
                            </Button>
                        )}
                    </Nav>
                </Navbar.Collapse>
            </Container>
        </Navbar>
    );
}