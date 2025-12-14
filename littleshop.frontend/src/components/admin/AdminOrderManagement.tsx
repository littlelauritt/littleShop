import { useEffect, useState } from 'react';
import { Table, Badge, Button, Spinner, Alert, Pagination } from 'react-bootstrap';
import { shipOrderAdmin, cancelOrderAdmin, OrderResponse } from '../../services/orderService';
import { authenticatedFetch } from '../../assets/api';

// 1. Extendemos la interfaz
interface SafeOrderResponse extends OrderResponse {
    totalAmount?: number;
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
                total = 1;
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
            setMsg('Pedido marcado como ENVIADO 🚚');
            fetchOrders(currentPage);
        } catch { setMsg('Error al enviar.'); }
    };

    const handleCancel = async (id: number) => {
        if (!confirm('¿Cancelar pedido?')) return;
        try {
            await cancelOrderAdmin(id);
            setMsg('Pedido CANCELADO ❌');
            fetchOrders(currentPage);
        } catch { setMsg('Error al cancelar.'); }
    };

    // Helper de seguridad para el precio
    const getSafeTotal = (order: SafeOrderResponse) => {
        return (order.total !== undefined ? order.total : order.totalAmount) || 0;
    };

    if (loading && orders.length === 0) return <div className="text-center p-4"><Spinner animation="border" /></div>;

    return (
        <div>
            {msg && <Alert variant="info" dismissible onClose={() => setMsg('')}>{msg}</Alert>}

            <h3 className="mb-3">Panel de Administración 👮‍♂️</h3>

            <Table striped bordered hover responsive>
                <thead className="table-dark">
                    <tr>
                        <th>ID</th>
                        <th>User ID</th>
                        <th>Total</th>
                        <th>Estado</th>
                        <th>Acciones</th>
                    </tr>
                </thead>
                <tbody>
                    {orders.length === 0 ? <tr><td colSpan={5} className="text-center">No hay pedidos.</td></tr> :
                        orders.map(o => (
                            <tr key={o.id}>
                                <td>{o.id}</td>
                                <td style={{ fontSize: '0.8rem', maxWidth: '150px', overflow: 'hidden', textOverflow: 'ellipsis' }} title={o.userId}>
                                    {o.customerEmail || o.userId}
                                </td>

                                {/* Precio seguro */}
                                <td className="fw-bold">{getSafeTotal(o).toFixed(2)} €</td>

                                <td>
                                    <Badge bg={
                                        o.status === 'Shipped' ? 'success' :
                                            o.status === 'Cancelled' ? 'danger' :
                                                o.status === 'Confirmed' ? 'primary' : 'warning'
                                    }>
                                        {o.status}
                                    </Badge>
                                </td>
                                <td>
                                    {/* CORRECCIÓN: Botones visibles si no está enviado ni cancelado */}
                                    {(o.status !== 'Shipped' && o.status !== 'Cancelled') && (
                                        <>
                                            <Button variant="success" size="sm" className="me-1" onClick={() => handleShip(o.id)}>
                                                🚚 Enviar
                                            </Button>
                                            <Button variant="danger" size="sm" onClick={() => handleCancel(o.id)}>
                                                ✖
                                            </Button>
                                        </>
                                    )}
                                </td>
                            </tr>
                        ))}
                </tbody>
            </Table>

            {totalPages > 1 && (
                <div className="d-flex justify-content-center mt-3">
                    <Pagination>
                        <Pagination.Prev onClick={() => setCurrentPage(p => Math.max(1, p - 1))} disabled={currentPage === 1} />
                        <Pagination.Item active>{currentPage} / {totalPages}</Pagination.Item>
                        <Pagination.Next onClick={() => setCurrentPage(p => Math.min(totalPages, p + 1))} disabled={currentPage === totalPages} />
                    </Pagination>
                </div>
            )}
        </div>
    );
}