import { authenticatedFetch } from "../assets/api";

export interface Product {
    id: number;
    name: string;
    description: string;
    price: number;
    stock: number;
    imageUrl?: string;
}

export async function getProducts(): Promise<Product[]> {
    // LLama al Gateway -> YARP redirige a Catalog
    return await authenticatedFetch<Product[]>('/api/v1/products', 'GET');
}

export async function getProductById(id: number): Promise<Product> {
    return await authenticatedFetch<Product>(`/api/v1/products/${id}`, 'GET');
}