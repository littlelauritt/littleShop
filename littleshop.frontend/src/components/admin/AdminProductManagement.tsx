import React, { useEffect, useState } from 'react';
import { Table, Button, Modal, Form, Alert, Spinner, Badge } from 'react-bootstrap';
import { getProducts, createProduct, updateProduct, deleteProduct, Product, ProductDto } from '../../services/productService';

export default function AdminProductManagement() {
    const [products, setProducts] = useState<Product[]>([]);
    const [loading, setLoading] = useState(true);
    const [msg, setMsg] = useState<{ text: string, type: 'success' | 'danger' } | null>(null);

    // Estados del Modal
    const [showModal, setShowModal] = useState(false);
    const [editingId, setEditingId] = useState<number | null>(null); // Null = Creando, Number = Editando

    // Formulario
    const [formData, setFormData] = useState<ProductDto>({
        name: '',
        description: '',
        price: 0,
        stock: 0
    });

    const fetchProducts = async () => {
        setLoading(true);
        try {
            const data = await getProducts();
            // Ordenamos por ID para que no bailen al editar
            setProducts(data.sort((a, b) => a.id - b.id));
        } catch (error) {
            setMsg({ text: 'Error al cargar productos.', type: 'danger' });
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => { fetchProducts(); }, []);

    // Abrir Modal (Crear o Editar)
    const handleOpenModal = (product?: Product) => {
        if (product) {
            setEditingId(product.id);
            setFormData({
                name: product.name,
                description: product.description || '',
                price: product.price,
                stock: product.stock
            });
        } else {
            setEditingId(null);
            setFormData({ name: '', description: '', price: 0, stock: 0 });
        }
        setShowModal(true);
    };

    // Guardar (Submit)
    const handleSave = async (e: React.FormEvent) => {
        e.preventDefault();
        try {
            if (editingId) {
                await updateProduct(editingId, formData);
                setMsg({ text: 'Producto actualizado correctamente.', type: 'success' });
            } else {
                await createProduct(formData);
                setMsg({ text: 'Producto creado correctamente.', type: 'success' });
            }
            setShowModal(false);
            fetchProducts();
        } catch (error) {
            setMsg({ text: 'Error al guardar el producto.', type: 'danger' });
        }
    };

    // Borrar
    const handleDelete = async (id: number) => {
        if (!confirm('¿Seguro que quieres eliminar este producto?')) return;
        try {
            await deleteProduct(id);
            setMsg({ text: 'Producto eliminado.', type: 'success' });
            fetchProducts();
        } catch (error) {
            setMsg({ text: 'Error al eliminar. Puede que tenga pedidos asociados.', type: 'danger' });
        }
    };

    if (loading) return <Spinner animation="border" />;

    return (
        <div>
            <div className="d-flex justify-content-between align-items-center mb-3">
                <h4>Inventario de Productos</h4>
                <Button variant="primary" onClick={() => handleOpenModal()}>
                    + Nuevo Producto
                </Button>
            </div>

            {msg && <Alert variant={msg.type} onClose={() => setMsg(null)} dismissible>{msg.text}</Alert>}

            <Table striped bordered hover responsive>
                <thead>
                    <tr>
                        <th>ID</th>
                        <th>Nombre</th>
                        <th>Precio</th>
                        <th>Stock</th>
                        <th>Acciones</th>
                    </tr>
                </thead>
                <tbody>
                    {products.map(p => (
                        <tr key={p.id}>
                            <td>{p.id}</td>
                            <td>
                                <strong>{p.name}</strong>
                                <div className="text-muted small">{p.description?.substring(0, 50)}...</div>
                            </td>
                            <td>{p.price.toFixed(2)} €</td>
                            <td>
                                <Badge bg={p.stock > 10 ? 'success' : p.stock > 0 ? 'warning' : 'danger'}>
                                    {p.stock}
                                </Badge>
                            </td>
                            <td>
                                <Button variant="outline-primary" size="sm" className="me-2" onClick={() => handleOpenModal(p)}>
                                    ✏️ Editar
                                </Button>
                                <Button variant="outline-danger" size="sm" onClick={() => handleDelete(p.id)}>
                                    🗑️ Borrar
                                </Button>
                            </td>
                        </tr>
                    ))}
                </tbody>
            </Table>

            {/* MODAL DE CREACIÓN / EDICIÓN */}
            <Modal show={showModal} onHide={() => setShowModal(false)}>
                <Modal.Header closeButton>
                    <Modal.Title>{editingId ? 'Editar Producto' : 'Nuevo Producto'}</Modal.Title>
                </Modal.Header>
                <Form onSubmit={handleSave}>
                    <Modal.Body>
                        <Form.Group className="mb-3">
                            <Form.Label>Nombre</Form.Label>
                            <Form.Control
                                type="text"
                                required
                                value={formData.name}
                                onChange={e => setFormData({ ...formData, name: e.target.value })}
                            />
                        </Form.Group>
                        <Form.Group className="mb-3">
                            <Form.Label>Descripción</Form.Label>
                            <Form.Control
                                as="textarea"
                                rows={2}
                                value={formData.description}
                                onChange={e => setFormData({ ...formData, description: e.target.value })}
                            />
                        </Form.Group>
                        <div className="row">
                            <div className="col-6">
                                <Form.Group className="mb-3">
                                    <Form.Label>Precio (€)</Form.Label>
                                    <Form.Control
                                        type="number"
                                        step="0.01"
                                        min="0"
                                        required
                                        value={formData.price}
                                        onChange={e => setFormData({ ...formData, price: parseFloat(e.target.value) })}
                                    />
                                </Form.Group>
                            </div>
                            <div className="col-6">
                                <Form.Group className="mb-3">
                                    <Form.Label>Stock</Form.Label>
                                    <Form.Control
                                        type="number"
                                        min="0"
                                        required
                                        value={formData.stock}
                                        onChange={e => setFormData({ ...formData, stock: parseInt(e.target.value) })}
                                    />
                                </Form.Group>
                            </div>
                        </div>
                    </Modal.Body>
                    <Modal.Footer>
                        <Button variant="secondary" onClick={() => setShowModal(false)}>Cancelar</Button>
                        <Button variant="primary" type="submit">Guardar</Button>
                    </Modal.Footer>
                </Form>
            </Modal>
        </div>
    );
}