import { authenticatedFetch } from "../assets/api";

// DTOs
export interface OrderItemDto {
    productId: number;
    productName: string;
    quantity: number;
    unitPrice: number;
}

export interface CreateOrderRequest {
    items: OrderItemDto[];
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

export async function createOrder(items: OrderItemDto[]): Promise<OrderResponse> {
    // CORREGIDO: Solo pasamos path, método y body (3 argumentos)
    return await authenticatedFetch<OrderResponse>('/api/v1/orders', 'POST', { items });
}

export async function getMyOrders(): Promise<OrderResponse[]> {
    // CORREGIDO: 2 argumentos (body es opcional)
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
    // POST /api/v1/orders/admin/{id}/cancel
    await authenticatedFetch(`/api/v1/orders/admin/${orderId}/cancel`, 'POST');
}