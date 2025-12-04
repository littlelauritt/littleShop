import React, { useEffect, useState } from 'react';
import { Table, Button, Modal, Form, Alert, Spinner, Badge, Pagination } from 'react-bootstrap';
// Quitamos getProducts porque lo haremos manual para paginar
import { createProduct, updateProduct, deleteProduct, Product, ProductDto } from '../../services/productService';

const GATEWAY_URL = import.meta.env.VITE_GATEWAY_URL;

// Interfaz para manejar la respuesta flexible del backend (evita el uso de 'any')
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

    // Estados de Paginación
    const [currentPage, setCurrentPage] = useState(1);
    const [totalPages, setTotalPages] = useState(1);
    const pageSize = 10; // Tamaño de página para admin

    // Estados del Modal
    const [showModal, setShowModal] = useState(false);
    const [editingId, setEditingId] = useState<number | null>(null);

    // Formulario
    const [formData, setFormData] = useState<ProductDto>({
        name: '',
        description: '',
        price: 0,
        stock: 0
    });

    useEffect(() => { 
        fetchProducts(currentPage); 
    }, [currentPage]);

    // Función de carga "Blindada" (Igual que en Home.tsx)
    const fetchProducts = async (page: number) => {
        setLoading(true);
        try {
            const response = await fetch(`${GATEWAY_URL}/api/v1/products?page=${page}&pageSize=${pageSize}`);
            if (!response.ok) throw new Error("Error al cargar productos");

            // CORRECCIÓN ESLINT: Tipamos la respuesta en lugar de usar 'any'
            const data = await response.json() as Product[] | PagedApiResponse;
            
            let productList: Product[] = [];
            let total = 1;

            // Lógica de detección de formato (Array vs Objeto Paginado)
            if (Array.isArray(data)) {
                // Caso A: Array directo (formato antiguo)
                productList = data;
                total = 1;
            } else {
                // Caso B: Objeto Paginado (formato nuevo)
                // TypeScript ahora sabe que si no es array, es PagedApiResponse
                productList = data.items || data.Items || [];
                total = data.totalPages || data.TotalPages || 1;
            }

            // Ordenamos por ID para que la tabla no baile
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

    // Abrir Modal
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
            fetchProducts(currentPage); // Recargamos la página actual
        } catch {
            setMsg({ text: 'Error al guardar el producto.', type: 'danger' });
        }
    };

    // Borrar
    const handleDelete = async (id: number) => {
        if (!confirm('¿Seguro que quieres eliminar este producto?')) return;
        try {
            await deleteProduct(id);
            setMsg({ text: 'Producto eliminado.', type: 'success' });
            fetchProducts(currentPage);
        } catch {
            setMsg({ text: 'Error al eliminar. Puede que tenga pedidos asociados.', type: 'danger' });
        }
    };

    if (loading && products.length === 0) return <div className="text-center p-5"><Spinner animation="border" /></div>;

    return (
        <div>
            <div className="d-flex justify-content-between align-items-center mb-3">
                <h4>Inventario de Productos</h4>
                <Button variant="primary" onClick={() => handleOpenModal()}>
                    + Nuevo Producto
                </Button>
            </div>

            {msg && <Alert variant={msg.type} onClose={() => setMsg(null)} dismissible>{msg.text}</Alert>}

            <Table striped bordered hover responsive className="align-middle">
                <thead className="bg-light">
                    <tr>
                        <th style={{width: '50px'}}>ID</th>
                        <th>Nombre</th>
                        <th style={{width: '100px'}}>Precio</th>
                        <th style={{width: '80px'}}>Stock</th>
                        <th style={{width: '180px'}}>Acciones</th>
                    </tr>
                </thead>
                <tbody>
                    {products.length === 0 ? (
                        <tr><td colSpan={5} className="text-center py-4">No hay productos.</td></tr>
                    ) : (
                        products.map(p => (
                            <tr key={p.id}>
                                <td>{p.id}</td>
                                <td>
                                    <strong>{p.name}</strong>
                                    <div className="text-muted small text-truncate" style={{maxWidth: '250px'}}>
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
                                    <Button variant="outline-primary" size="sm" className="me-2" onClick={() => handleOpenModal(p)}>
                                        ✏️ Editar
                                    </Button>
                                    <Button variant="outline-danger" size="sm" onClick={() => handleDelete(p.id)}>
                                        🗑️ Borrar
                                    </Button>
                                </td>
                            </tr>
                        ))
                    )}
                </tbody>
            </Table>

            {/* CONTROLES DE PAGINACIÓN */}
            {totalPages > 1 && (
                <div className="d-flex justify-content-center mt-3">
                    <Pagination>
                        <Pagination.Prev 
                            onClick={() => setCurrentPage(p => Math.max(1, p - 1))} 
                            disabled={currentPage === 1} 
                        />
                        <Pagination.Item active>{currentPage} / {totalPages}</Pagination.Item>
                        <Pagination.Next 
                            onClick={() => setCurrentPage(p => Math.min(totalPages, p + 1))} 
                            disabled={currentPage === totalPages} 
                        />
                    </Pagination>
                </div>
            )}

            {/* MODAL DE CREACIÓN / EDICIÓN */}
            <Modal show={showModal} onHide={() => setShowModal(false)} backdrop="static">
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