import { useEffect, useState } from 'react';
import { Table, Badge, Button, Spinner, Alert, Pagination, Modal, Form } from 'react-bootstrap';
import { shipOrderAdmin, cancelOrderAdmin, OrderResponse } from '../../services/orderService';
import { authenticatedFetch } from '../../assets/api';

interface SafeOrderResponse extends OrderResponse {
    totalAmount?: number;
    cancellationRequested?: boolean;
    cancellationRequestedAt?: string;
    cancellationReason?: string;
}

interface PagedOrderResponse {
    items?: SafeOrderResponse[];
    Items?: SafeOrderResponse[];
    totalPages?: number;
    TotalPages?: number;
}

export default function AdminOrderManagement() {
    const [orders, setOrders] = useState<SafeOrderResponse[]>([]);
    const [loading, setLoading] = useState(true);
    const [msg, setMsg] = useState('');

    const [currentPage, setCurrentPage] = useState(1);
    const [totalPages, setTotalPages] = useState(1);
    const pageSize = 10;

    const [showModal, setShowModal] = useState(false);
    const [selectedOrder, setSelectedOrder] = useState<SafeOrderResponse | null>(null);

    const [showOnlyCancellationRequests, setShowOnlyCancellationRequests] = useState(false);

    const fetchOrders = async (page: number) => {
        setLoading(true);
        try {
            const data = await authenticatedFetch<SafeOrderResponse[] | PagedOrderResponse>(
                `/api/v1/orders/admin?page=${page}&pageSize=${pageSize}`,
                'GET'
            );

            let list: SafeOrderResponse[] = [];
            let total = 1;

            if (Array.isArray(data)) {
                list = data;
            } else {
                list = data.items || data.Items || [];
                total = data.totalPages || data.TotalPages || 1;
            }

            setOrders(list.sort((a, b) => b.id - a.id));
            setTotalPages(total);

        } catch (error) {
            console.error(error);
            setMsg('Error al cargar pedidos. Verifica permisos.');
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => { fetchOrders(currentPage); }, [currentPage]);

    const handleShip = async (id: number) => {
        if (!confirm('¿Marcar como ENVIADO?')) return;
        try {
            await shipOrderAdmin(id);
            setMsg('Pedido marcado como ENVIADO');
            fetchOrders(currentPage);
        } catch {
            setMsg('Error al enviar.');
        }
    };

    const handleCancel = async (id: number) => {
        if (!confirm('¿Cancelar este pedido? Esta acción es irreversible.')) return;
        try {
            await cancelOrderAdmin(id);
            setMsg('Pedido CANCELADO correctamente');
            fetchOrders(currentPage);
            setShowModal(false);
        } catch {
            setMsg('Error al cancelar.');
        }
    };

    const handleShowDetails = (order: SafeOrderResponse) => {
        setSelectedOrder(order);
        setShowModal(true);
    };

    const getSafeTotal = (order: SafeOrderResponse) => {
        return (order.total !== undefined ? order.total : order.totalAmount) || 0;
    };

    const filteredOrders = showOnlyCancellationRequests
        ? orders.filter(o => o.cancellationRequested && o.status !== 'Cancelled')
        : orders;

    if (loading && orders.length === 0) return <div className="text-center p-5"><Spinner animation="border" variant="primary" /></div>;

    return (
        <div>
            {msg && <Alert variant="info" dismissible onClose={() => setMsg('')}>{msg}</Alert>}

            <div className="d-flex justify-content-between align-items-center mb-4">
                <h4 className="mb-0 text-dark">Gestión de Pedidos</h4>

                <Form.Check
                    type="switch"
                    id="filter-cancellations"
                    label="Solo solicitudes de cancelación"
                    checked={showOnlyCancellationRequests}
                    onChange={(e) => setShowOnlyCancellationRequests(e.target.checked)}
                    className="text-muted"
                />
            </div>

            <div className="table-responsive">
                <Table hover className="align-middle">
                    <thead className="bg-light">
                        <tr>
                            <th className="border-0 py-3 ps-3">ID</th>
                            <th className="border-0 py-3">Cliente</th>
                            <th className="border-0 py-3">Total</th>
                            <th className="border-0 py-3">Estado</th>
                            <th className="border-0 py-3 pe-3">Acciones</th>
                        </tr>
                    </thead>
                    <tbody>
                        {filteredOrders.length === 0 ? (
                            <tr>
                                <td colSpan={5} className="text-center py-5 text-muted">
                                    {showOnlyCancellationRequests
                                        ? 'No hay solicitudes de cancelación pendientes.'
                                        : 'No hay pedidos registrados.'}
                                </td>
                            </tr>
                        ) : (
                            filteredOrders.map(o => (
                                <tr key={o.id}>
                                    <td className="ps-3 fw-bold">#{o.id}</td>
                                    <td className="text-muted small" style={{ maxWidth: '200px' }} title={o.userId}>
                                        {o.customerEmail || o.userId}
                                    </td>
                                    <td className="fw-bold text-dark">{getSafeTotal(o).toFixed(2)} €</td>
                                    <td>
                                        <div className="d-flex flex-column align-items-start gap-1">
                                            {/* Estado Base */}
                                            {o.status === 'Shipped' && <Badge bg="success" className="fw-normal">Enviado</Badge>}
                                            {o.status === 'Cancelled' && <Badge bg="danger" className="fw-normal">Cancelado</Badge>}
                                            {o.status === 'Confirmed' && <Badge bg="primary" className="fw-normal">Confirmado</Badge>}
                                            {o.status === 'Pending' && <Badge bg="secondary" className="fw-normal bg-opacity-50 text-dark">Pendiente</Badge>}

                                            {/* Estado Solicitud (FUERTE) */}
                                            {o.cancellationRequested && o.status !== 'Cancelled' && (
                                                <Badge style={{ backgroundColor: '#fd7e14', color: 'white', border: '1px solid #e8590c' }}>
                                                    ⚠️ Solicitud Cancelación
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
                                                Ver
                                            </Button>

                                            {(o.status !== 'Shipped' && o.status !== 'Cancelled') && (
                                                <>
                                                    {o.cancellationRequested ? (
                                                        <Button
                                                            variant="danger"
                                                            size="sm"
                                                            onClick={() => handleCancel(o.id)}
                                                            style={{ fontWeight: '600' }}
                                                        >
                                                            Aprobar Cancelación
                                                        </Button>
                                                    ) : (
                                                        <>
                                                            <Button variant="success" size="sm" onClick={() => handleShip(o.id)}>
                                                                Enviar
                                                            </Button>
                                                            <Button variant="outline-danger" size="sm" onClick={() => handleCancel(o.id)}>
                                                                Cancelar
                                                            </Button>
                                                        </>
                                                    )}
                                                </>
                                            )}
                                        </div>
                                    </td>
                                </tr>
                            ))
                        )}
                    </tbody>
                </Table>
            </div>

            {totalPages > 1 && (
                <div className="d-flex justify-content-center mt-4">
                    <Pagination>
                        <Pagination.Prev onClick={() => setCurrentPage(p => Math.max(1, p - 1))} disabled={currentPage === 1} />
                        <Pagination.Item active>{currentPage} / {totalPages}</Pagination.Item>
                        <Pagination.Next onClick={() => setCurrentPage(p => Math.min(totalPages, p + 1))} disabled={currentPage === totalPages} />
                    </Pagination>
                </div>
            )}

            {/* MODAL DE DETALLES */}
            <Modal show={showModal} onHide={() => setShowModal(false)} size="lg" centered>
                <Modal.Header closeButton className="border-0 pb-0">
                    <Modal.Title className="h5">Detalles del Pedido #{selectedOrder?.id}</Modal.Title>
                </Modal.Header>
                <Modal.Body>
                    {selectedOrder ? (
                        <div>
                            <div className="d-flex justify-content-between mb-4 text-muted small border-bottom pb-3">
                                <span>Cliente: <strong>{selectedOrder.customerEmail}</strong></span>
                                <span>Fecha: {new Date(selectedOrder.createdAt).toLocaleString()}</span>
                            </div>

                            {selectedOrder.cancellationRequested && selectedOrder.status !== 'Cancelled' && (
                                <Alert variant="warning" className="mb-4 border-0 bg-warning bg-opacity-10">
                                    <div className="d-flex justify-content-between align-items-center">
                                        <div>
                                            <strong className="text-warning-emphasis">Razón de cancelación:</strong>
                                            <p className="mb-0 mt-1 text-dark small">{selectedOrder.cancellationReason || 'No especificada'}</p>
                                        </div>
                                        <Button variant="danger" size="sm" onClick={() => handleCancel(selectedOrder.id)}>
                                            Aceptar y Cancelar
                                        </Button>
                                    </div>
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
                                <span className="text-muted me-3">Total del pedido:</span>
                                <span className="h4 text-primary fw-bold">{getSafeTotal(selectedOrder).toFixed(2)} €</span>
                            </div>
                        </div>
                    ) : (
                        <div className="text-center py-4"><Spinner animation="border" size="sm" /></div>
                    )}
                </Modal.Body>
                <Modal.Footer className="border-0 pt-0">
                    <Button variant="secondary" onClick={() => setShowModal(false)}>Cerrar</Button>
                </Modal.Footer>
            </Modal>
        </div>
    );
}