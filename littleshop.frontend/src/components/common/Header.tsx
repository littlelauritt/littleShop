import { useNavigate, useLocation } from 'react-router-dom'; // 1. Importamos useLocation
import { getToken, logout } from '../../assets/utils/auth';
import { useState, useEffect } from 'react';

export default function Header() {
    const navigate = useNavigate();
    const location = useLocation(); // 2. Obtenemos la ubicación actual
    const [token, setToken] = useState<string | null>(getToken());

    const handleLogout = () => {
        logout();
        setToken(null);
        navigate('/login');
    };

    // 3. Este useEffect se ejecuta cada vez que cambias de ruta (location)
    useEffect(() => {
        // Revisa si hay token cada vez que el usuario navega
        setToken(getToken());
    }, [location]);

    return (
        <nav className="navbar navbar-expand-lg navbar-dark bg-dark">
            <div className="container-fluid">
                <a className="navbar-brand" href="/">LittleShop</a>
                <div className="collapse navbar-collapse">
                    <ul className="navbar-nav me-auto mb-2 mb-lg-0">
                        {token && (
                            <>
                                <li className="nav-item">
                                    <button
                                        className="nav-link btn btn-link"
                                        onClick={() => navigate('/profile')}
                                    >
                                        Mi Perfil
                                    </button>
                                </li>
                                <li className="nav-item">
                                    <button
                                        className="nav-link btn btn-link"
                                        onClick={() => navigate('/admin')}
                                    >
                                        Admin Dashboard
                                    </button>
                                </li>
                            </>
                        )}
                    </ul>
                    <ul className="navbar-nav">
                        {token ? (
                            <button className="btn btn-outline-danger" onClick={handleLogout}>Logout</button>
                        ) : (
                            <button className="btn btn-outline-success" onClick={() => navigate('/login')}>Login</button>
                        )}
                    </ul>
                </div>
            </div>
        </nav>
    );
}