import React, { useEffect, useState } from 'react';
import { Table, Button, Modal, Form, Alert, Spinner, Badge, Pagination } from 'react-bootstrap';
import { createProduct, updateProduct, deleteProduct, Product, ProductDto } from '../../services/productService';

const GATEWAY_URL = import.meta.env.VITE_GATEWAY_URL;

interface PagedApiResponse {
    items?: Product[];
    Items?: Product[];
    totalPages?: number;
    TotalPages?: number;
}

export default function AdminProductManagement() {
    const [products, setProducts] = useState<Product[]>([]);
    const [loading, setLoading] = useState(true);
    const [msg, setMsg] = useState<{ text: string, type: 'success' | 'danger' } | null>(null);

    const [currentPage, setCurrentPage] = useState(1);
    const [totalPages, setTotalPages] = useState(1);
    const pageSize = 10;

    const [showModal, setShowModal] = useState(false);
    const [editingId, setEditingId] = useState<number | null>(null);

    // ✅ Formulario actualizado con imageUrl
    const [formData, setFormData] = useState<ProductDto>({
        name: '',
        description: '',
        price: 0,
        stock: 0,
        imageUrl: '' // Nuevo campo
    });

    useEffect(() => { fetchProducts(currentPage); }, [currentPage]);

    const fetchProducts = async (page: number) => {
        setLoading(true);
        try {
            const response = await fetch(`${GATEWAY_URL}/api/v1/products?page=${page}&pageSize=${pageSize}`);
            if (!response.ok) throw new Error("Error al cargar productos");

            const data = await response.json() as Product[] | PagedApiResponse;

            let productList: Product[] = [];
            let total = 1;

            if (Array.isArray(data)) {
                productList = data;
                total = 1;
            } else {
                productList = data.items || data.Items || [];
                total = data.totalPages || data.TotalPages || 1;
            }

            setProducts(productList.sort((a, b) => a.id - b.id));
            setTotalPages(total);
        } catch (error) {
            console.error(error);
            setMsg({ text: 'Error al cargar productos.', type: 'danger' });
            setProducts([]);
        } finally {
            setLoading(false);
        }
    };

    const handleOpenModal = (product?: Product) => {
        if (product) {
            setEditingId(product.id);
            setFormData({
                name: product.name,
                description: product.description || '',
                price: product.price,
                stock: product.stock,
                imageUrl: product.imageUrl || '' // Cargar imagen existente
            });
        } else {
            setEditingId(null);
            setFormData({ name: '', description: '', price: 0, stock: 0, imageUrl: '' });
        }
        setShowModal(true);
    };

    const handleSave = async (e: React.FormEvent) => {
        e.preventDefault();
        try {
            if (editingId) {
                await updateProduct(editingId, formData);
                setMsg({ text: 'Producto actualizado.', type: 'success' });
            } else {
                await createProduct(formData);
                setMsg({ text: 'Producto creado.', type: 'success' });
            }
            setShowModal(false);
            fetchProducts(currentPage);
        } catch {
            setMsg({ text: 'Error al guardar.', type: 'danger' });
        }
    };

    const handleDelete = async (id: number) => {
        if (!confirm('¿Seguro que quieres eliminar este producto?')) return;
        try {
            await deleteProduct(id);
            setMsg({ text: 'Producto eliminado.', type: 'success' });
            fetchProducts(currentPage);
        } catch {
            setMsg({ text: 'Error al eliminar.', type: 'danger' });
        }
    };

    if (loading && products.length === 0) return <div className="text-center p-5"><Spinner animation="border" /></div>;

    return (
        <div>
            <div className="d-flex justify-content-between align-items-center mb-3">
                <h4>Inventario de Productos</h4>
                <Button variant="primary" onClick={() => handleOpenModal()}>+ Nuevo Producto</Button>
            </div>

            {msg && <Alert variant={msg.type} onClose={() => setMsg(null)} dismissible>{msg.text}</Alert>}

            <Table striped bordered hover responsive className="align-middle">
                <thead>
                    <tr>
                        <th style={{ width: '50px' }}>ID</th>
                        <th style={{ width: '80px' }}>Img</th> {/* Nueva Columna */}
                        <th>Nombre / Descripción</th>
                        <th style={{ width: '100px' }}>Precio</th>
                        <th style={{ width: '80px' }}>Stock</th>
                        <th style={{ width: '180px' }}>Acciones</th>
                    </tr>
                </thead>
                <tbody>
                    {products.length === 0 ? (
                        <tr><td colSpan={6} className="text-center py-4">No hay productos.</td></tr>
                    ) : (
                        products.map(p => (
                            <tr key={p.id}>
                                <td>{p.id}</td>
                                {/* ✅ Columna de Imagen */}
                                <td className="text-center">
                                    {p.imageUrl ? (
                                        <img src={p.imageUrl} alt="mini" style={{ width: '40px', height: '40px', objectFit: 'cover', borderRadius: '4px' }} />
                                    ) : (
                                        <span className="text-muted">🚫</span>
                                    )}
                                </td>
                                <td>
                                    <strong>{p.name}</strong>
                                    <div className="text-muted small text-truncate" style={{ maxWidth: '300px' }}>
                                        {p.description}
                                    </div>
                                </td>
                                <td>{p.price.toFixed(2)} €</td>
                                <td>
                                    <Badge bg={p.stock > 10 ? 'success' : p.stock > 0 ? 'warning' : 'danger'}>
                                        {p.stock}
                                    </Badge>
                                </td>
                                <td>
                                    <Button variant="primary" size="sm" className="me-2" onClick={() => handleOpenModal(p)}>✏️</Button>
                                    <Button variant="outline-danger" size="sm" onClick={() => handleDelete(p.id)}>🗑️</Button>
                                </td>
                            </tr>
                        ))
                    )}
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

            {/* MODAL */}
            <Modal show={showModal} onHide={() => setShowModal(false)} backdrop="static" size="lg">
                <Modal.Header closeButton>
                    <Modal.Title>{editingId ? 'Editar Producto' : 'Nuevo Producto'}</Modal.Title>
                </Modal.Header>
                <Form onSubmit={handleSave}>
                    <Modal.Body>
                        <div className="row">
                            <div className="col-md-8">
                                <Form.Group className="mb-3">
                                    <Form.Label>Nombre</Form.Label>
                                    <Form.Control type="text" required value={formData.name} onChange={e => setFormData({ ...formData, name: e.target.value })} />
                                </Form.Group>
                            </div>
                            <div className="col-md-4">
                                <Form.Group className="mb-3">
                                    <Form.Label>Stock</Form.Label>
                                    <Form.Control type="number" min="0" required value={formData.stock} onChange={e => setFormData({ ...formData, stock: parseInt(e.target.value) })} />
                                </Form.Group>
                            </div>
                        </div>

                        {/* ✅ Nuevo campo de URL de Imagen */}
                        <Form.Group className="mb-3">
                            <Form.Label>URL de la Imagen (Opcional)</Form.Label>
                            <Form.Control
                                type="text"
                                placeholder="https://ejemplo.com/imagen.jpg"
                                value={formData.imageUrl || ''}
                                onChange={e => setFormData({ ...formData, imageUrl: e.target.value })}
                            />
                            {formData.imageUrl && (
                                <div className="mt-2 p-2 border rounded bg-light text-center">
                                    <small className="d-block text-muted mb-1">Vista previa:</small>
                                    <img src={formData.imageUrl} alt="Vista previa" style={{ maxHeight: '100px' }} onError={(e) => (e.currentTarget.style.display = 'none')} />
                                </div>
                            )}
                        </Form.Group>

                        <Form.Group className="mb-3">
                            <Form.Label>Descripción</Form.Label>
                            <Form.Control as="textarea" rows={4} value={formData.description} onChange={e => setFormData({ ...formData, description: e.target.value })} />
                        </Form.Group>

                        <Form.Group className="mb-3">
                            <Form.Label>Precio (€)</Form.Label>
                            <Form.Control type="number" step="0.01" min="0" required value={formData.price} onChange={e => setFormData({ ...formData, price: parseFloat(e.target.value) })} />
                        </Form.Group>

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