import { useEffect, useState } from 'react';
import { Container, Row, Col, Card, Spinner, Alert, Button } from 'react-bootstrap';
import { getProducts, Product } from '../services/productService';
import { useCart } from '../context/CartContext';

export default function Home() {
    const { addToCart } = useCart();
    const [products, setProducts] = useState<Product[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        loadProducts();
    }, []);

    const loadProducts = async () => {
        try {
            const data = await getProducts();
            setProducts(data);
        } catch (err) {
            setError(err instanceof Error ? err.message : 'Error al cargar productos');
        } finally {
            setLoading(false);
        }
    };

    if (loading) return <div className="text-center mt-5"><Spinner animation="border" /></div>;

    if (error) return (
        <Container className="mt-5">
            <Alert variant="danger">
                <h4>Error de conexión</h4>
                <p>{error}</p>
                <small>Verifica que el proyecto .NET Aspire esté corriendo.</small>
            </Alert>
        </Container>
    );

    return (
        <Container className="mt-5">
            <div className="text-center mb-5">
                <h1>Bienvenido a LittleShop 🛍️</h1>
                <p className="lead">Tu tienda de confianza.</p>
            </div>

            <Row>
                {products.length === 0 ? (
                    <div className="text-center">No hay productos disponibles.</div>
                ) : (
                    products.map(p => (
                        <Col key={p.id} md={4} className="mb-4">
                            <Card className="h-100 shadow-sm">
                                <Card.Body className="d-flex flex-column">
                                    <Card.Title>{p.name}</Card.Title>
                                    <Card.Text className="text-muted flex-grow-1">
                                        {p.description}
                                    </Card.Text>

                                    {/* SECCIÓN DE PRECIO Y STOCK */}
                                    <div className="d-flex justify-content-between align-items-center mt-3 mb-3">
                                        <h4 className="text-primary mb-0">
                                            {p.price.toFixed(2)} €
                                        </h4>
                                        <span className={`badge ${p.stock > 0 ? 'bg-success' : 'bg-danger'}`}>
                                            {p.stock > 0 ? `Stock: ${p.stock}` : 'Agotado'}
                                        </span>
                                    </div>

                                    {/* BOTÓN AÑADIR AL CARRITO */}
                                    <Button
                                        variant="primary"
                                        className="w-100"
                                        disabled={p.stock <= 0}
                                        onClick={() => addToCart(p)}
                                    >
                                        {p.stock > 0 ? 'Añadir al carrito 🛒' : 'Agotado'}
                                    </Button>

                                </Card.Body>
                            </Card>
                        </Col>
                    ))
                )}
            </Row>
        </Container>
    );
}