import { authenticatedFetch } from "../assets/api";

export interface Product {
    id: number;
    name: string;
    description: string;
    price: number;
    stock: number;
    imageUrl?: string;
}

export interface ProductDto {
    name: string;
    description: string;
    price: number;
    stock: number;
    imageUrl?: string;
}


export async function getProducts(): Promise<Product[]> {
    return await authenticatedFetch<Product[]>('/api/v1/products', 'GET');
}

export async function getProductById(id: number): Promise<Product> {
    return await authenticatedFetch<Product>(`/api/v1/products/${id}`, 'GET');
}


export async function createProduct(product: ProductDto): Promise<Product> {
    return await authenticatedFetch<Product>('/api/v1/products', 'POST', product);
}

export async function updateProduct(id: number, product: ProductDto): Promise<Product> {
    return await authenticatedFetch<Product>(`/api/v1/products/${id}`, 'PUT', product);
}

export async function deleteProduct(id: number): Promise<void> {
    await authenticatedFetch(`/api/v1/products/${id}`, 'DELETE');
}