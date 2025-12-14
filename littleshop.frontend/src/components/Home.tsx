import { useEffect, useState } from 'react';
import { Container, Row, Col, Card, Button, Spinner, Alert, Form } from 'react-bootstrap';
import { useCart } from '../context/CartContext';
import { Product } from '../types';

interface PagedResponse {
    items: Product[];
    totalCount: number;
    pageNumber: number;
    pageSize: number;
    totalPages: number;
}

const GATEWAY_URL = import.meta.env.VITE_GATEWAY_URL;

export default function Home() {
    const [products, setProducts] = useState<Product[]>([]);

    // Estados
    const [currentPage, setCurrentPage] = useState(1);
    const [totalPages, setTotalPages] = useState(0);
    const pageSize = 8;
    const [sortOrder, setSortOrder] = useState(''); // ✅ Estado del filtro

    const [loading, setLoading] = useState(true);
    const [error, setError] = useState('');

    const { addToCart } = useCart();

    // ✅ Efecto: Se dispara al cambiar página O cambiar filtro
    useEffect(() => {
        fetchProducts(currentPage, sortOrder);
    }, [currentPage, sortOrder]);

    const fetchProducts = async (page: number, sort: string) => {
        setLoading(true);
        setError('');
        try {
            // ✅ Mandamos el parámetro sort al backend
            let url = `${GATEWAY_URL}/api/v1/products?page=${page}&pageSize=${pageSize}`;
            if (sort) {
                url += `&sort=${sort}`;
            }

            const response = await fetch(url);
            if (!response.ok) throw new Error('Error al cargar productos');

            const data: PagedResponse = await response.json();
            setProducts(data.items);
            setTotalPages(data.totalPages);

        } catch (err) {
            setError('No se pudieron cargar los productos. Por favor, intenta más tarde.');
            console.error(err);
        } finally {
            setLoading(false);
        }
    };

    const handlePrev = () => { if (currentPage > 1) setCurrentPage(p => p - 1); };
    const handleNext = () => { if (currentPage < totalPages) setCurrentPage(p => p + 1); };

    if (loading) return (
        <div className="d-flex flex-column justify-content-center align-items-center" style={{ minHeight: '50vh' }}>
            <Spinner animation="border" variant="primary" />
            <p className="mt-3 text-muted">Cargando catálogo...</p>
        </div>
    );

    if (error) return (
        <Container className="mt-5">
            <Alert variant="danger">{error}</Alert>
        </Container>
    );

    return (
        <Container>
            {/* Barra superior de controles */}
            <div className="d-flex justify-content-between align-items-center mb-4 mt-3">

                {/* ✅ Selector de filtros */}
                <div style={{ minWidth: '220px' }}>
                    <Form.Select
                        value={sortOrder}
                        onChange={(e) => {
                            setSortOrder(e.target.value);
                            setCurrentPage(1); // Reset a pág 1 al filtrar
                        }}
                        className="shadow-sm border-0 bg-white text-secondary"
                        style={{ cursor: 'pointer', borderRadius: '12px' }}
                    >
                        <option value="">Ordenar por...</option>
                        <option value="price-asc">💰 Precio: Menor a Mayor</option>
                        <option value="price-desc">💰 Precio: Mayor a Menor</option>
                        <option value="name-asc">🔤 Nombre: A - Z</option>
                        <option value="name-desc">🔤 Nombre: Z - A</option>
                    </Form.Select>
                </div>

                <span className="text-muted small">
                    Página {currentPage} de {totalPages}
                </span>
            </div>

            {products.length === 0 ? (
                <Alert variant="info" className="text-center">
                    No hay productos disponibles en este momento.
                </Alert>
            ) : (
                <Row>
                    {products.map((product) => (
                        <Col key={product.id} md={6} lg={4} xl={3} className="mb-4">
                            {/* Card arreglada visualmente */}
                            <Card className="h-100 shadow-sm border-0 overflow-hidden">
                                <div style={{
                                    height: '220px',
                                    padding: '20px',
                                    display: 'flex',
                                    alignItems: 'center',
                                    justifyContent: 'center',
                                    backgroundColor: '#fff',
                                    position: 'relative'
                                }}>
                                    {product.imageUrl ? (
                                        <Card.Img
                                            variant="top"
                                            src={product.imageUrl}
                                            alt={product.name}
                                            style={{
                                                maxHeight: '100%',
                                                maxWidth: '100%',
                                                objectFit: 'contain'
                                            }}
                                        />
                                    ) : (
                                        <div className="text-center text-muted">
                                            <span style={{ fontSize: '3rem', opacity: 0.3 }}>📦</span>
                                            <p className="small m-0">Sin imagen</p>
                                        </div>
                                    )}
                                </div>

                                <Card.Body className="d-flex flex-column pt-0">
                                    <Card.Title className="fs-6 fw-bold text-dark mb-2 text-truncate" title={product.name}>
                                        {product.name}
                                    </Card.Title>

                                    <Card.Text className="text-muted small flex-grow-1" style={{ whiteSpace: 'pre-wrap', fontSize: '0.85rem' }}>
                                        {product.description}
                                    </Card.Text>

                                    <div className="mt-3">
                                        <div className="d-flex justify-content-between align-items-center mb-3">
                                            <span className="fs-5 fw-bold text-primary">{product.price.toFixed(2)}€</span>
                                            <span className={`badge ${product.stock > 0 ? 'bg-success' : 'bg-danger'} fw-normal`}>
                                                {product.stock > 0 ? `${product.stock} ud.` : 'Agotado'}
                                            </span>
                                        </div>

                                        <Button
                                            variant="primary"
                                            className="w-100"
                                            disabled={product.stock === 0}
                                            onClick={() => addToCart(product)}
                                        >
                                            {product.stock > 0 ? 'Añadir al carrito' : 'Sin Stock'}
                                        </Button>
                                    </div>
                                </Card.Body>
                            </Card>
                        </Col>
                    ))}
                </Row>
            )}

            {totalPages > 1 && (
                <div className="d-flex justify-content-center gap-2 my-5">
                    <Button variant="outline-secondary" size="sm" onClick={handlePrev} disabled={currentPage === 1} className="px-3">
                        Anterior
                    </Button>
                    <Button variant="outline-secondary" size="sm" onClick={handleNext} disabled={currentPage === totalPages} className="px-3">
                        Siguiente
                    </Button>
                </div>
            )}
        </Container>
    );
}