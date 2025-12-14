import { useEffect, useState } from 'react';
import { Container, Row, Col, Card, Button, Spinner, Alert } from 'react-bootstrap';
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
    const [currentPage, setCurrentPage] = useState(1);
    const [totalPages, setTotalPages] = useState(0);
    const pageSize = 8;
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState('');
    const { addToCart } = useCart();

    useEffect(() => { fetchProducts(currentPage); }, [currentPage]);

    const fetchProducts = async (page: number) => {
        setLoading(true);
        setError('');
        try {
            const response = await fetch(`${GATEWAY_URL}/api/v1/products?page=${page}&pageSize=${pageSize}`);
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
            <div className="d-flex justify-content-end align-items-center mb-4 mt-3">
                <span className="text-muted small me-3">
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
                            {/* ✅ CAMBIO: overflow-hidden y border-0 para que la imagen respete los bordes redondos */}
                            <Card className="h-100 shadow-sm border-0 overflow-hidden">

                                {/* Contenedor de imagen limpio */}
                                <div style={{
                                    height: '220px',
                                    padding: '20px',
                                    display: 'flex',
                                    alignItems: 'center',
                                    justifyContent: 'center',
                                    backgroundColor: '#fff', // Fondo blanco para que la imagen (si es PNG transparente) se vea bien
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