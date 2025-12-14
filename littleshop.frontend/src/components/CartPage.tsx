import { useState } from 'react';
import { Container, Table, Button, Alert, Spinner } from 'react-bootstrap';
import { useNavigate } from 'react-router-dom';
import { useCart } from '../context/CartContext';
import { createOrder, OrderItemDto } from '../services/orderService';
import { getToken } from '../assets/utils/auth';

export default function CartPage() {
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
            // Mapeamos el carrito al DTO que espera el backend
            const itemsToSend: OrderItemDto[] = cart.map(item => ({
                productId: item.id,
                productName: item.name,
                quantity: item.quantity,
                unitPrice: item.price
            }));

            await createOrder(itemsToSend);

            clearCart();
            setMsg({ text: 'Pedido realizado con éxito. Redirigiendo...', type: 'success' });

            // Redirigir al perfil para ver el pedido
            setTimeout(() => navigate('/profile'), 2000);

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
                <div className="py-5">
                    <h3 className="text-muted mb-3">Tu carrito está vacío</h3>
                    <p className="text-secondary">Parece que aún no has añadido ningún juguete.</p>
                    <Button variant="primary" onClick={() => navigate('/')}>
                        Volver al catálogo
                    </Button>
                </div>
            </Container>
        );
    }

    return (
        <Container className="mt-5">
            <h2 className="mb-4">Tu Carrito de Compra</h2>

            {msg && <Alert variant={msg.type}>{msg.text}</Alert>}

            {cart.length > 0 && (
                <>
                    <div className="table-responsive">
                        <Table hover className="align-middle">
                            <thead className="bg-light">
                                <tr>
                                    <th className="border-0 py-3 ps-3">Producto</th>
                                    <th className="border-0 py-3 text-end">Precio</th>
                                    <th className="border-0 py-3 text-center">Cant.</th>
                                    <th className="border-0 py-3 text-end">Subtotal</th>
                                    <th className="border-0 py-3 text-end pe-3">Acción</th>
                                </tr>
                            </thead>
                            <tbody>
                                {cart.map(item => (
                                    <tr key={item.id}>
                                        <td className="ps-3 fw-bold text-secondary">
                                            {item.name}
                                        </td>
                                        <td className="text-end">
                                            {item.price.toFixed(2)} €
                                        </td>
                                        <td className="text-center">
                                            {item.quantity}
                                        </td>
                                        <td className="text-end fw-bold text-primary">
                                            {(item.price * item.quantity).toFixed(2)} €
                                        </td>
                                        <td className="text-end pe-3">
                                            <Button
                                                variant="outline-danger"
                                                size="sm"
                                                className="border-0"
                                                onClick={() => removeFromCart(item.id)}
                                                title="Eliminar producto"
                                            >
                                                Eliminar
                                            </Button>
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                        </Table>
                    </div>

                    <div className="d-flex flex-column align-items-end mt-4 pt-3 border-top">
                        <h3 className="mb-3 text-secondary">
                            Total: <span className="text-dark fw-bold">{cartTotal.toFixed(2)} €</span>
                        </h3>
                        <div className="d-flex gap-3">
                            <Button variant="secondary" onClick={() => navigate('/')}>
                                Seguir comprando
                            </Button>
                            <Button
                                variant="primary"
                                onClick={handleCheckout}
                                disabled={loading}
                                className="px-4"
                            >
                                {loading ? (
                                    <>
                                        <Spinner animation="border" size="sm" className="me-2" />
                                        Procesando...
                                    </>
                                ) : (
                                    'Confirmar y Pagar'
                                )}
                            </Button>
                        </div>
                    </div>
                </>
            )}
        </Container>
    );
}