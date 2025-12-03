import { useNavigate, useLocation } from 'react-router-dom';
import { getToken, logout, getUserRole, getUserEmail } from '../../assets/utils/auth';
import { useState, useEffect } from 'react';
import { useCart } from '../../context/CartContext';

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
        <nav className="navbar navbar-expand-lg navbar-dark bg-dark">
            <div className="container-fluid">
                <a className="navbar-brand" href="/" onClick={(e) => { e.preventDefault(); navigate('/'); }}>
                    LittleShop 🛍️
                </a>

                <button
                    className="btn btn-outline-light position-relative me-3 ms-auto"
                    onClick={() => navigate('/cart')}
                >
                    🛒 Carrito
                    {cartCount > 0 && (
                        <span className="position-absolute top-0 start-100 translate-middle badge rounded-pill bg-danger">
                            {cartCount}
                        </span>
                    )}
                </button>

                <div className="collapse navbar-collapse flex-grow-0">
                    <ul className="navbar-nav me-auto mb-2 mb-lg-0 align-items-center">
                        {token ? (
                            <>
                                <li className="nav-item me-2">
                                    <span className="navbar-text text-light fst-italic" style={{ fontSize: '0.9rem' }}>
                                        {email}
                                    </span>
                                </li>

                                {/* LÓGICA DE VISIBILIDAD DE BOTONES */}
                                {role === 'Admin' ? (
                                    // SI ES ADMIN: Solo ve el botón al Panel
                                    <li className="nav-item">
                                        <button className="nav-link btn btn-link text-warning fw-bold" onClick={() => navigate('/admin')}>
                                            ⚙️ Panel Admin
                                        </button>
                                    </li>
                                ) : (
                                    // SI ES USER: Ve su perfil normal
                                    <li className="nav-item">
                                        <button className="nav-link btn btn-link" onClick={() => navigate('/profile')}>
                                            👤 Mi Perfil
                                        </button>
                                    </li>
                                )}

                                <li className="nav-item">
                                    <button className="btn btn-outline-danger ms-2 btn-sm" onClick={handleLogout}>
                                        Salir
                                    </button>
                                </li>
                            </>
                        ) : (
                            <li className="nav-item">
                                <button className="btn btn-outline-success ms-2" onClick={() => navigate('/login')}>Login</button>
                            </li>
                        )}
                    </ul>
                </div>
            </div>
        </nav>
    );
}