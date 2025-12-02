import { Container, Table, Button, Alert, Spinner } from 'react-bootstrap';
import { useCart } from '../context/CartContext';
import { createOrder, OrderItemDto } from '../services/orderService';
import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { getToken } from '../assets/utils/auth';

export default function CartPage() {
    // Usamos cartTotal (asegúrate de haber actualizado CartContext.tsx también)
    const { cart, removeFromCart, clearCart, cartTotal } = useCart();

    const [loading, setLoading] = useState(false);
    const [msg, setMsg] = useState<{ text: string, type: 'success' | 'danger' } | null>(null);
    const navigate = useNavigate();

    const handleCheckout = async () => {
        if (!getToken()) {
            navigate('/login');
            return;
        }

        setLoading(true);
        setMsg(null);

        try {
            // Mapeamos el carrito al DTO del backend
            const itemsToSend: OrderItemDto[] = cart.map(item => ({
                productId: item.id,
                productName: item.name,
                quantity: item.quantity,
                unitPrice: item.price
            }));

            await createOrder(itemsToSend);

            clearCart();
            setMsg({ text: '¡Pedido realizado con éxito! 🚀', type: 'success' });

            setTimeout(() => navigate('/profile'), 3000);

        } catch (error) {
            console.error(error);
            setMsg({ text: (error as Error).message || 'Error al procesar el pedido.', type: 'danger' });
        } finally {
            setLoading(false);
        }
    };

    if (cart.length === 0 && !msg) {
        return (
            <Container className="mt-5 text-center">
                <h3>Tu carrito está vacío 🛒</h3>
                <Button variant="link" onClick={() => navigate('/')}>Volver al catálogo</Button>
            </Container>
        );
    }

    return (
        <Container className="mt-5">
            <h2>Tu Carrito de Compra</h2>
            {msg && <Alert variant={msg.type}>{msg.text}</Alert>}

            {cart.length > 0 && (
                <>
                    <Table striped bordered hover className="mt-4">
                        <thead>
                            <tr>
                                <th>Producto</th>
                                <th>Precio Unitario</th>
                                <th>Cantidad</th>
                                <th>Subtotal</th>
                                <th>Acción</th>
                            </tr>
                        </thead>
                        <tbody>
                            {cart.map(item => (
                                <tr key={item.id}>
                                    <td>{item.name}</td>
                                    <td>{item.price.toFixed(2)} €</td>
                                    <td>{item.quantity}</td>
                                    <td>{(item.price * item.quantity).toFixed(2)} €</td>
                                    <td>
                                        <Button variant="danger" size="sm" onClick={() => removeFromCart(item.id)}>
                                            X
                                        </Button>
                                    </td>
                                </tr>
                            ))}
                        </tbody>
                    </Table>
                    <div className="d-flex justify-content-end align-items-center gap-3 mt-3">
                        <h3>Total: {cartTotal.toFixed(2)} €</h3>
                        <Button
                            variant="success"
                            size="lg"
                            onClick={handleCheckout}
                            disabled={loading}
                        >
                            {loading ? <Spinner animation="border" size="sm" /> : 'Confirmar y Pagar ✅'}
                        </Button>
                    </div>
                </>
            )}
        </Container>
    );
}