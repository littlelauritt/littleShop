import { authenticatedFetch } from "../assets/api";

// DTOs
export interface OrderItemDto {
    productId: number;
    productName: string;
    quantity: number;
    unitPrice: number;
}

// CORRECCIÓN 1: Añadimos shippingAddress a la interfaz
export interface CreateOrderRequest {
    items: OrderItemDto[];
    shippingAddress: string;
}

export interface OrderResponse {
    id: number;
    userId: string;
    createdAt: string;
    total: number;
    status: string;
    items: OrderItemDto[];
    customerEmail?: string;
}

// --- FUNCIONES CLIENTE ---

// CORRECCIÓN 2: Modificamos la función para incluir la dirección
// Por ahora ponemos una dirección por defecto ("Calle Falsa 123") para que funcione ya.
// En el futuro, puedes pasarla como parámetro si tienes un formulario para ello.
export async function createOrder(items: OrderItemDto[]): Promise<OrderResponse> {
    const body: CreateOrderRequest = {
        items,
        shippingAddress: "Calle Principal 1, Madrid" // <--- ¡AQUÍ ESTABA EL PROBLEMA!
    };

    return await authenticatedFetch<OrderResponse>('/api/v1/orders', 'POST', body);
}

export async function getMyOrders(): Promise<OrderResponse[]> {
    return await authenticatedFetch<OrderResponse[]>('/api/v1/orders', 'GET');
}

export async function cancelOrder(orderId: number): Promise<void> {
    return await authenticatedFetch(`/api/v1/orders/${orderId}/cancel`, 'POST');
}

// --- FUNCIONES ADMIN ---

export async function getAllOrdersAdmin(): Promise<OrderResponse[]> {
    return await authenticatedFetch<OrderResponse[]>('/api/v1/orders/admin', 'GET');
}

export async function shipOrderAdmin(orderId: number): Promise<void> {
    return await authenticatedFetch(`/api/v1/orders/admin/${orderId}/ship`, 'POST');
}

export async function cancelOrderAdmin(orderId: number): Promise<void> {
    await authenticatedFetch(`/api/v1/orders/admin/${orderId}/cancel`, 'POST');
}