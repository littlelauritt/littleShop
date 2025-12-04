import { useEffect, useState } from 'react';
import { Container, Row, Col, Card, Button, Spinner, Alert, Badge } from 'react-bootstrap';
// Asegúrate de que esta ruta sea correcta según tu proyecto
import { useCart } from '../context/CartContext'; 

// Definimos la interfaz del Producto
interface Product {
    id: number;
    name: string;
    description: string;
    price: number;
    stock: number;
}

// Definimos la interfaz de la respuesta paginada
interface PagedResponse {
    items: Product[];
    totalCount: number;
    pageNumber: number;
    pageSize: number;
    totalPages: number;
}

const GATEWAY_URL = import.meta.env.VITE_GATEWAY_URL;

export default function Home() {
    // Estado para los productos
    const [products, setProducts] = useState<Product[]>([]);
    
    // Estados para paginación
    const [currentPage, setCurrentPage] = useState(1);
    const [totalPages, setTotalPages] = useState(0);
    const pageSize = 8; // Puedes cambiar esto a 10, 20, etc.

    // Estados de UI
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState('');

    const { addToCart } = useCart();

    useEffect(() => {
        fetchProducts(currentPage);
    }, [currentPage]);

    const fetchProducts = async (page: number) => {
        setLoading(true);
        setError('');
        try {
            // Llamamos al endpoint paginado pasando page y pageSize
            const response = await fetch(`${GATEWAY_URL}/api/v1/products?page=${page}&pageSize=${pageSize}`);
            
            if (!response.ok) throw new Error('Error al cargar productos');

            const data: PagedResponse = await response.json();

            // AQUÍ ESTÁ EL CAMBIO CLAVE:
            // Antes hacías: setProducts(data)
            // Ahora hacemos: setProducts(data.items)
            setProducts(data.items);
            setTotalPages(data.totalPages);

        } catch (err) {
            setError('No se pudieron cargar los productos. Intenta refrescar.');
            console.error(err);
        } finally {
            setLoading(false);
        }
    };

    const handlePrev = () => {
        if (currentPage > 1) setCurrentPage(p => p - 1);
    };

    const handleNext = () => {
        if (currentPage < totalPages) setCurrentPage(p => p + 1);
    };

    if (loading) return (
        <div className="text-center mt-5">
            <Spinner animation="border" variant="primary" />
            <p className="mt-2 text-muted">Cargando catálogo...</p>
        </div>
    );

    if (error) return <Alert variant="danger" className="mt-4">{error}</Alert>;

    return (
        <Container>
            <div className="d-flex justify-content-between align-items-center mb-4 mt-2">
                <h1>Catálogo</h1>
                <Badge bg="secondary">Página {currentPage} de {totalPages}</Badge>
            </div>

            {products.length === 0 ? (
                <Alert variant="info">No hay productos disponibles.</Alert>
            ) : (
                <Row>
                    {products.map((product) => (
                        <Col key={product.id} md={6} lg={4} xl={3} className="mb-4">
                            <Card className="h-100 shadow-sm hover-shadow transition-all">
                                {/* Si tuvieras imágenes, irían aquí */}
                                <div className="bg-light p-4 text-center text-muted" style={{height: '150px', display: 'flex', alignItems: 'center', justifyContent: 'center'}}>
                                    📦
                                </div>
                                <Card.Body className="d-flex flex-column">
                                    <Card.Title className="text-truncate" title={product.name}>
                                        {product.name}
                                    </Card.Title>
                                    <Card.Text className="text-muted small flex-grow-1" style={{minHeight: '3em'}}>
                                        {product.description?.substring(0, 60)}...
                                    </Card.Text>
                                    
                                    <div className="d-flex justify-content-between align-items-center mt-3">
                                        <h5 className="mb-0 text-primary">{product.price.toFixed(2)}€</h5>
                                        <Badge bg={product.stock > 0 ? "success" : "danger"}>
                                            {product.stock > 0 ? `Stock: ${product.stock}` : "Agotado"}
                                        </Badge>
                                    </div>

                                    <Button 
                                        variant="dark" 
                                        className="w-100 mt-3"
                                        disabled={product.stock === 0}
                                        onClick={() => addToCart(product)}
                                    >
                                        {product.stock > 0 ? 'Añadir al Carrito' : 'Sin Stock'}
                                    </Button>
                                </Card.Body>
                            </Card>
                        </Col>
                    ))}
                </Row>
            )}

            {/* CONTROLES DE PAGINACIÓN */}
            {totalPages > 1 && (
                <div className="d-flex justify-content-center gap-3 my-4">
                    <Button 
                        variant="outline-primary" 
                        onClick={handlePrev} 
                        disabled={currentPage === 1}
                    >
                        &larr; Anterior
                    </Button>
                    
                    <span className="align-self-center font-weight-bold">
                        {currentPage} / {totalPages}
                    </span>

                    <Button 
                        variant="outline-primary" 
                        onClick={handleNext} 
                        disabled={currentPage === totalPages}
                    >
                        Siguiente &rarr;
                    </Button>
                </div>
            )}
        </Container>
    );
}