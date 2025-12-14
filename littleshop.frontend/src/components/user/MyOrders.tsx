import { useEffect, useState } from 'react';
import { Table, Badge, Button, Spinner, Alert, Modal } from 'react-bootstrap';
import { getMyOrders, cancelOrder, OrderResponse } from '../../services/orderService';

// Extendemos la interfaz para aceptar ambos nombres por seguridad
interface SafeOrderResponse extends OrderResponse {
    totalAmount?: number;
}

export default function MyOrders() {
    const [orders, setOrders] = useState<SafeOrderResponse[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState('');

    // --- ESTADOS PARA EL MODAL DE DETALLES ---
    const [showModal, setShowModal] = useState(false);
    const [selectedOrder, setSelectedOrder] = useState<SafeOrderResponse | null>(null);

    const fetchOrders = async () => {
        try {
            const data = await getMyOrders();
            console.log("📦 Pedidos recibidos:", data); // Para depuración
            // Ordenamos por ID descendente (el más nuevo arriba)
            setOrders(data.sort((a, b) => b.id - a.id));
        } catch (err) {
            console.error(err);
            setError('Error al cargar pedidos.');
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => { fetchOrders(); }, []);

    const handleCancel = async (id: number) => {
        if (!confirm('¿Seguro que quieres cancelar este pedido?')) return;
        try {
            await cancelOrder(id);
            fetchOrders();
        } catch {
            alert('No se pudo cancelar el pedido. Verifica que no esté ya enviado.');
        }
    };

    // Función para abrir el modal
    const handleShowDetails = (order: SafeOrderResponse) => {
        setSelectedOrder(order);
        setShowModal(true);
    };

    // Helper para obtener el total de forma segura
    const getSafeTotal = (order: SafeOrderResponse) => {
        return (order.total !== undefined ? order.total : order.totalAmount) || 0;
    };

    if (loading) return <Spinner animation="border" />;
    if (error) return <Alert variant="danger">{error}</Alert>;
    if (orders.length === 0) return <Alert variant="info">No tienes pedidos aún.</Alert>;

    return (
        <>
            <h3>Mis Pedidos 📦</h3>
            <Table hover responsive striped>
                <thead>
                    <tr>
                        <th>#</th>
                        <th>Fecha</th>
                        <th>Total</th>
                        <th>Estado</th>
                        <th>Acciones</th>
                    </tr>
                </thead>
                <tbody>
                    {orders.map(o => (
                        <tr key={o.id}>
                            <td>{o.id}</td>
                            <td>{new Date(o.createdAt).toLocaleDateString()}</td>
                            {/* AQUÍ ESTABA EL ERROR: Usamos la función segura */}
                            <td>{getSafeTotal(o).toFixed(2)} €</td>
                            <td>
                                <Badge bg={
                                    o.status === 'Cancelled' ? 'danger' :
                                        o.status === 'Shipped' ? 'success' :
                                            'warning'
                                }>
                                    {o.status}
                                </Badge>
                            </td>
                            <td>
                                {/* BOTÓN DETALLES */}
                                <Button
                                    variant="info"
                                    size="sm"
                                    className="me-2 text-white"
                                    onClick={() => handleShowDetails(o)}
                                >
                                    👁️ Detalles
                                </Button>

                                {/* BOTÓN CANCELAR (Solo si no está enviado ni cancelado) */}
                                {(o.status === 'Confirmed' || o.status === 'Pending') && (
                                    <Button variant="outline-danger" size="sm" onClick={() => handleCancel(o.id)}>
                                        Cancelar
                                    </Button>
                                )}
                            </td>
                        </tr>
                    ))}
                </tbody>
            </Table>

            {/* --- VENTANA MODAL (POPUP) --- */}
            <Modal show={showModal} onHide={() => setShowModal(false)} size="lg" centered>
                <Modal.Header closeButton>
                    <Modal.Title>Detalles del Pedido #{selectedOrder?.id}</Modal.Title>
                </Modal.Header>
                <Modal.Body>
                    {selectedOrder ? (
                        <div>
                            <div className="d-flex justify-content-between mb-3">
                                <span><strong>Fecha:</strong> {new Date(selectedOrder.createdAt).toLocaleString()}</span>
                                <span><strong>Estado:</strong> {selectedOrder.status}</span>
                            </div>

                            <hr />

                            <h5>Productos Comprados:</h5>
                            <Table size="sm" bordered>
                                <thead className="table-light">
                                    <tr>
                                        <th>Producto</th>
                                        <th className="text-center">Cant.</th>
                                        <th className="text-end">Precio Ud.</th>
                                        <th className="text-end">Subtotal</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {selectedOrder.items.map((item, idx) => (
                                        <tr key={idx}>
                                            <td>{item.productName}</td>
                                            <td className="text-center">{item.quantity}</td>
                                            <td className="text-end">{item.unitPrice.toFixed(2)} €</td>
                                            <td className="text-end">{(item.quantity * item.unitPrice).toFixed(2)} €</td>
                                        </tr>
                                    ))}
                                </tbody>
                            </Table>
                            <h4 className="text-end mt-4 text-primary">
                                {/* AQUÍ TAMBIÉN CORREGIDO */}
                                Total Pagado: {getSafeTotal(selectedOrder).toFixed(2)} €
                            </h4>
                        </div>
                    ) : (
                        <Spinner animation="border" />
                    )}
                </Modal.Body>
                <Modal.Footer>
                    <Button variant="secondary" onClick={() => setShowModal(false)}>
                        Cerrar
                    </Button>
                </Modal.Footer>
            </Modal>
        </>
    );
}