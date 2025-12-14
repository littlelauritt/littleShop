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

export async function createOrder(items: OrderItemDto[]): Promise<OrderResponse> {
    const body: CreateOrderRequest = {
        items,
        shippingAddress: "Calle Principal 1, Madrid"
    };
    return await authenticatedFetch<OrderResponse>('/api/v1/orders', 'POST', body);
}

export async function getMyOrders(): Promise<OrderResponse[]> {
    return await authenticatedFetch<OrderResponse[]>('/api/v1/orders', 'GET');
}

// ✅ NUEVO: Usuario solicita cancelación (NO cancela directamente)
export async function requestCancellation(orderId: number, reason: string): Promise<void> {    return await authenticatedFetch<void>(
        `/api/v1/orders/${orderId}/request-cancellation`,
        'POST',
        { reason }
    );
}

// --- FUNCIONES ADMIN ---

export async function getAllOrdersAdmin(): Promise<OrderResponse[]> {
    return await authenticatedFetch<OrderResponse[]>('/api/v1/orders/admin', 'GET');
}

export async function shipOrderAdmin(orderId: number): Promise<void> {
    return await authenticatedFetch<void>(`/api/v1/orders/admin/${orderId}/ship`, 'POST');
}

// ✅ CORREGIDO: Admin cancela pedido directamente
export async function cancelOrderAdmin(orderId: number): Promise<void> {
    return await authenticatedFetch<void>(`/api/v1/orders/admin/${orderId}/cancel`, 'POST');
}