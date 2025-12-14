import { useEffect, useState } from 'react';
import { Table, Badge, Button, Spinner, Alert, Modal, Form } from 'react-bootstrap';
import { getMyOrders, requestCancellation, OrderResponse } from '../../services/orderService';

// Extendemos la interfaz para evitar errores de tipo si el backend devuelve campos extra
interface SafeOrderResponse extends OrderResponse {
    totalAmount?: number;
    cancellationRequested?: boolean;
    cancellationRequestedAt?: string;
    cancellationReason?: string;
}

export default function MyOrders() {
    const [orders, setOrders] = useState<SafeOrderResponse[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState('');
    const [successMsg, setSuccessMsg] = useState(''); // Estado para mensajes de éxito

    // --- ESTADOS PARA MODALES ---
    const [showModal, setShowModal] = useState(false);
    const [selectedOrder, setSelectedOrder] = useState<SafeOrderResponse | null>(null);

    const [showCancelModal, setShowCancelModal] = useState(false);
    const [orderToCancel, setOrderToCancel] = useState<number | null>(null);
    const [cancellationReason, setCancellationReason] = useState('');
    const [cancelLoading, setCancelLoading] = useState(false);

    // Cargar pedidos
    const fetchOrders = async () => {
        try {
            const data = await getMyOrders();
            // Ordenar por ID descendente (más reciente primero)
            setOrders(data.sort((a, b) => b.id - a.id));
        } catch (err) {
            console.error(err);
            setError('No se pudieron cargar tus pedidos.');
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => { fetchOrders(); }, []);

    // Abrir modal de cancelación
    const handleOpenCancelModal = (id: number) => {
        setOrderToCancel(id);
        setCancellationReason('');
        setShowCancelModal(true);
    };

    // Confirmar cancelación
    const handleConfirmCancellation = async () => {
        if (!cancellationReason || !orderToCancel) return;

        setCancelLoading(true);
        try {
            await requestCancellation(orderToCancel, cancellationReason);
            setSuccessMsg('Solicitud enviada correctamente.');
            setShowCancelModal(false);
            fetchOrders(); // Recargar la lista para ver el nuevo estado
        } catch (err) {
            const errorMsg = (err as Error).message.toLowerCase();
            if (errorMsg.includes('ya existe')) {
                alert('Ya has solicitado la cancelación de este pedido.');
            } else {
                alert('No se pudo procesar la solicitud.');
            }
        } finally {
            setCancelLoading(false);
        }
    };

    // Ver detalles
    const handleShowDetails = (order: SafeOrderResponse) => {
        setSelectedOrder(order);
        setShowModal(true);
    };

    // Helper para total
    const getSafeTotal = (order: SafeOrderResponse) => {
        return (order.total !== undefined ? order.total : order.totalAmount) || 0;
    };

    if (loading) return <div className="text-center py-5"><Spinner animation="border" variant="primary" /></div>;

    if (orders.length === 0 && !loading) {
        return <Alert variant="light" className="text-center mt-4">Aún no has realizado ningún pedido.</Alert>;
    }

    return (
        <>
            <h3 className="mb-4 text-dark">Mis Pedidos</h3>

            {error && <Alert variant="danger">{error}</Alert>}
            {successMsg && <Alert variant="success" dismissible onClose={() => setSuccessMsg('')}>{successMsg}</Alert>}

            <div className="table-responsive">
                <Table hover className="align-middle">
                    <thead className="bg-light">
                        <tr>
                            <th className="border-0 py-3 ps-3">#</th>
                            <th className="border-0 py-3">Fecha</th>
                            <th className="border-0 py-3">Total</th>
                            <th className="border-0 py-3">Estado</th>
                            <th className="border-0 py-3 pe-3">Acciones</th>
                        </tr>
                    </thead>
                    <tbody>
                        {orders.map(o => (
                            <tr key={o.id}>
                                <td className="ps-3 text-secondary">#{o.id}</td>
                                <td className="text-muted">{new Date(o.createdAt).toLocaleDateString()}</td>
                                <td className="fw-bold">{getSafeTotal(o).toFixed(2)} €</td>
                                <td>
                                    <div className="d-flex flex-column align-items-start gap-1">
                                        {/* Estados normales */}
                                        {o.status === 'Cancelled' && <Badge bg="danger" className="fw-normal">Cancelado</Badge>}
                                        {o.status === 'Shipped' && <Badge bg="success" className="fw-normal">Enviado</Badge>}
                                        {o.status === 'Confirmed' && <Badge bg="primary" className="fw-normal">Confirmado</Badge>}
                                        {o.status === 'Pending' && <Badge bg="secondary" className="fw-normal bg-opacity-50 text-dark">Pendiente</Badge>}

                                        {/* Estado POTENTE de solicitud */}
                                        {o.cancellationRequested && o.status !== 'Cancelled' && (
                                            <Badge style={{
                                                backgroundColor: '#fd7e14',
                                                color: 'white',
                                                border: '1px solid #e8590c',
                                                padding: '6px 10px',
                                                fontSize: '0.8rem'
                                            }}>
                                                ⚠️ Solicitud de Cancelación
                                            </Badge>
                                        )}
                                    </div>
                                </td>
                                <td className="pe-3">
                                    <div className="d-flex gap-2">
                                        <Button
                                            variant="light"
                                            size="sm"
                                            className="border text-secondary"
                                            onClick={() => handleShowDetails(o)}
                                        >
                                            Ver Detalles
                                        </Button>

                                        {/* Botón Solicitar Cancelación (Solo si no está enviado ni cancelado) */}
                                        {(o.status === 'Confirmed' || o.status === 'Pending') && (
                                            <>
                                                {o.cancellationRequested ? (
                                                    <Button
                                                        variant="secondary"
                                                        size="sm"
                                                        disabled
                                                        style={{ opacity: 0.6 }}
                                                    >
                                                        Pendiente...
                                                    </Button>
                                                ) : (
                                                    <Button
                                                        variant="outline-danger"
                                                        size="sm"
                                                        onClick={() => handleOpenCancelModal(o.id)}
                                                    >
                                                        Solicitar Cancelación
                                                    </Button>
                                                )}
                                            </>
                                        )}
                                    </div>
                                </td>
                            </tr>
                        ))}
                    </tbody>
                </Table>
            </div>

            {/* --- MODAL DETALLES --- */}
            <Modal show={showModal} onHide={() => setShowModal(false)} size="lg" centered>
                <Modal.Header closeButton className="border-0">
                    <Modal.Title className="h5">Pedido #{selectedOrder?.id}</Modal.Title>
                </Modal.Header>
                <Modal.Body>
                    {selectedOrder && (
                        <div>
                            <div className="d-flex justify-content-between mb-3 text-muted small border-bottom pb-3">
                                <span>Fecha: {new Date(selectedOrder.createdAt).toLocaleString()}</span>
                                <span>Estado: <strong>{selectedOrder.status}</strong></span>
                            </div>

                            {/* Aviso dentro del modal si hay solicitud */}
                            {selectedOrder.cancellationRequested && (
                                <Alert variant="warning" className="small border-0 bg-warning bg-opacity-10 mb-4">
                                    <strong>Has solicitado cancelar este pedido.</strong>
                                    <br />
                                    Motivo: {selectedOrder.cancellationReason}
                                </Alert>
                            )}

                            <h6 className="mb-3 text-secondary">Productos</h6>
                            <Table size="sm" className="mb-0">
                                <thead className="bg-light text-secondary">
                                    <tr>
                                        <th className="border-0 fw-normal">Artículo</th>
                                        <th className="border-0 text-center fw-normal">Cant.</th>
                                        <th className="border-0 text-end fw-normal">Precio</th>
                                        <th className="border-0 text-end fw-normal">Total</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {selectedOrder.items.map((item, idx) => (
                                        <tr key={idx}>
                                            <td className="border-0">{item.productName}</td>
                                            <td className="border-0 text-center">{item.quantity}</td>
                                            <td className="border-0 text-end">{item.unitPrice.toFixed(2)} €</td>
                                            <td className="border-0 text-end fw-bold text-dark">{(item.quantity * item.unitPrice).toFixed(2)} €</td>
                                        </tr>
                                    ))}
                                </tbody>
                            </Table>

                            <div className="text-end mt-4 pt-3 border-top">
                                <span className="text-muted me-3">Total pagado:</span>
                                <span className="h4 text-primary fw-bold">{getSafeTotal(selectedOrder).toFixed(2)} €</span>
                            </div>
                        </div>
                    )}
                </Modal.Body>
                <Modal.Footer className="border-0 pt-0">
                    <Button variant="secondary" onClick={() => setShowModal(false)}>Cerrar</Button>
                </Modal.Footer>
            </Modal>

            {/* --- MODAL SOLICITAR CANCELACIÓN --- */}
            <Modal show={showCancelModal} onHide={() => setShowCancelModal(false)} centered>
                <Modal.Header closeButton className="border-0">
                    <Modal.Title className="h5">Solicitar Cancelación</Modal.Title>
                </Modal.Header>
                <Modal.Body>
                    <Alert variant="info" className="small border-0 bg-info bg-opacity-10 text-dark mb-4">
                        Un administrador revisará tu solicitud. Si el pedido no ha sido enviado, se aprobará la devolución.
                    </Alert>

                    <Form.Group>
                        <Form.Label className="small text-muted">Motivo de la cancelación:</Form.Label>
                        <Form.Select
                            value={cancellationReason}
                            onChange={(e) => setCancellationReason(e.target.value)}
                            disabled={cancelLoading}
                        >
                            <option value="">-- Selecciona un motivo --</option>
                            <option value="Ya no lo necesito">Ya no lo necesito</option>
                            <option value="Encontré un precio mejor">Encontré un precio mejor</option>
                            <option value="Me equivoqué al pedirlo">Me equivoqué al pedirlo</option>
                            <option value="Otro">Otro</option>
                        </Form.Select>
                    </Form.Group>
                </Modal.Body>
                <Modal.Footer className="border-0">
                    <Button variant="link" className="text-decoration-none text-muted" onClick={() => setShowCancelModal(false)}>
                        No, mantener pedido
                    </Button>
                    <Button
                        variant="warning"
                        onClick={handleConfirmCancellation}
                        disabled={!cancellationReason || cancelLoading}
                        className="text-white fw-bold"
                    >
                        {cancelLoading ? (
                            <>
                                <Spinner animation="border" size="sm" className="me-2" />
                                Enviando...
                            </>
                        ) : (
                            'Confirmar Solicitud'
                        )}
                    </Button>
                </Modal.Footer>
            </Modal>
        </>
    );
}