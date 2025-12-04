import { useEffect, useState } from 'react';
import { Table, Badge, Button, Spinner, Alert, Pagination } from 'react-bootstrap';
import { shipOrderAdmin, cancelOrderAdmin, OrderResponse } from '../../services/orderService';
import { authenticatedFetch } from '../../assets/api'; // Importamos el fetch autenticado

// Ya no necesitamos GATEWAY_URL aquí porque authenticatedFetch lo usa internamente

interface PagedOrderResponse {
    items?: OrderResponse[];
    Items?: OrderResponse[];
    totalPages?: number;
    TotalPages?: number;
}

export default function AdminOrderManagement() {
    const [orders, setOrders] = useState<OrderResponse[]>([]);
    const [loading, setLoading] = useState(true);
    const [msg, setMsg] = useState('');

    // Paginación
    const [currentPage, setCurrentPage] = useState(1);
    const [totalPages, setTotalPages] = useState(1);
    const pageSize = 10;

    const fetchOrders = async (page: number) => {
        setLoading(true);
        try {
            // USO DE AUTHENTICATED FETCH:
            // 1. Maneja el token automáticamente.
            // 2. Maneja la URL base del Gateway.
            // 3. Maneja errores 401 redirigiendo al login.
            const data = await authenticatedFetch<OrderResponse[] | PagedOrderResponse>(
                `/api/v1/orders/admin?page=${page}&pageSize=${pageSize}`, 
                'GET'
            );

            let list: OrderResponse[] = [];
            let total = 1;

            // Lógica Blindada (Array vs Paginado)
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
            setMsg('Error al cargar pedidos. Verifica que tengas permisos de Admin.');
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => { fetchOrders(currentPage); }, [currentPage]);

    const handleShip = async (id: number) => {
        try {
            await shipOrderAdmin(id);
            setMsg('Pedido marcado como ENVIADO 🚚');
            fetchOrders(currentPage);
        } catch { setMsg('Error al enviar.'); }
    };

    const handleCancel = async (id: number) => {
        if (!confirm('¿Cancelar pedido de usuario?')) return;
        try {
            await cancelOrderAdmin(id);
            setMsg('Pedido CANCELADO ❌');
            fetchOrders(currentPage);
        } catch { setMsg('Error al cancelar.'); }
    };

    if (loading && orders.length === 0) return <div className="text-center p-4"><Spinner animation="border" /></div>;

    return (
        <div>
            {msg && <Alert variant="info" dismissible onClose={() => setMsg('')}>{msg}</Alert>}
            
            <Table striped bordered hover responsive>
                <thead>
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
                                {o.userId}
                            </td>
                            <td>{o.total.toFixed(2)} €</td>
                            <td>
                                <Badge bg={o.status === 'Shipped' ? 'success' : o.status === 'Cancelled' ? 'danger' : 'warning'}>
                                    {o.status}
                                </Badge>
                            </td>
                            <td>
                                {(o.status === 'Pending' || o.status === 'Confirmed') && (
                                    <>
                                        <Button variant="success" size="sm" className="me-1" onClick={() => handleShip(o.id)}>Enviar</Button>
                                        <Button variant="danger" size="sm" onClick={() => handleCancel(o.id)}>X</Button>
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