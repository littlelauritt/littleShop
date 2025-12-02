import { useEffect, useState } from 'react';
import { Table, Badge, Button, Spinner, Alert } from 'react-bootstrap';
import { getAllOrdersAdmin, shipOrderAdmin, cancelOrderAdmin, OrderResponse } from '../../services/orderService';
export default function AdminOrderManagement() {
    const [orders, setOrders] = useState<OrderResponse[]>([]);
    const [loading, setLoading] = useState(true);
    const [msg, setMsg] = useState('');

    const fetchOrders = async () => {
        setLoading(true);
        try {
            const data = await getAllOrdersAdmin();
            setOrders(data.sort((a, b) => b.id - a.id));
        } catch (error) {
            console.error(error);
            setMsg('Error al cargar pedidos.');
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => { fetchOrders(); }, []);

    const handleShip = async (id: number) => {
        try {
            await shipOrderAdmin(id);
            setMsg('Pedido marcado como ENVIADO 🚚');
            fetchOrders();
        } catch { setMsg('Error al enviar.'); }
    };

    const handleCancel = async (id: number) => {
        if (!confirm('¿Cancelar pedido de usuario?')) return;
        try {
            // 2. Usar la función de Admin
            await cancelOrderAdmin(id);

            setMsg('Pedido CANCELADO ❌');
            fetchOrders();
        } catch { setMsg('Error al cancelar.'); }
    };

    if (loading) return <Spinner animation="border" />;

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
                    {orders.map(o => (
                        <tr key={o.id}>
                            <td>{o.id}</td>
                            <td style={{ fontSize: '0.8rem', maxWidth: '150px', overflow: 'hidden', textOverflow: 'ellipsis' }}>
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
        </div>
    );
}