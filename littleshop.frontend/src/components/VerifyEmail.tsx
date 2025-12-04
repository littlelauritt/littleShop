import { useState, useEffect, useCallback } from "react";
// Importamos el hook para leer la URL
import { useSearchParams, useNavigate } from "react-router-dom";
import { verifyUser } from "../assets/api";

export default function VerifyEmail() {
    const [status, setStatus] = useState('verifying'); 
    
    // Hook para leer los parámetros ?userId=...&code=...
    const [searchParams] = useSearchParams();
    const navigate = useNavigate();

    // Extraemos los datos de la URL
    const userId = searchParams.get("userId");
    const code = searchParams.get("code");

    const verifyAccount = useCallback(async (uid: string, c: string) => {
        try {
            console.log("Enviando verificación al backend...", { uid, c });
            await verifyUser(uid, c);
            
            setStatus('success');
            // Opcional: Redirigir al login después de 3 segundos
            setTimeout(() => navigate('/login'), 3000);

        } catch (error) {
            console.error(error);
            setStatus('error');
        }
    }, [navigate]); 

    useEffect(() => {
        // Solo intentamos verificar si tenemos los datos
        if (userId && code) {
            verifyAccount(userId, code);
        } else {
            console.error("Faltan parámetros en la URL");
            setStatus('error');
        }
    }, [userId, code, verifyAccount]); 

    return (
        <div className="container mt-5 text-center">
            <div className="card shadow-sm p-4 mx-auto" style={{ maxWidth: '400px' }}>
                {status === 'verifying' && (
                    <>
                        <div className="spinner-border text-primary mb-3" role="status"></div>
                        <h4>Verificando tu cuenta...</h4>
                        <p className="text-muted">Por favor espera un momento.</p>
                    </>
                )}
                
                {status === 'success' && (
                    <>
                        <h1 className="text-success mb-3">✅</h1>
                        <h4>¡Email Verificado!</h4>
                        <p>Tu cuenta ha sido activada correctamente.</p>
                        <button onClick={() => navigate('/login')} className="btn btn-primary mt-2">
                            Ir al Login
                        </button>
                    </>
                )}
                
                {status === 'error' && (
                    <>
                        <h1 className="text-danger mb-3">❌</h1>
                        <h4>Error de Verificación</h4>
                        <p>El enlace es inválido o ha expirado.</p>
                        <p className="text-small text-muted">ID: {userId || 'N/A'}</p>
                    </>
                )}
            </div>
        </div>
    );
}