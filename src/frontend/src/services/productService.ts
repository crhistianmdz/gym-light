import { fetchWithAuth } from '@/services/httpClient';

export interface CreateProductRequest {
  name: string;
  sku?: string;
  description?: string;
  price: number;
  initialStock: number;
}

export interface UpdateProductRequest {
  name?: string;
  description?: string;
  price?: number;
  stock?: number;
}

export const productService = {
  async createProduct(data: CreateProductRequest): Promise<void> {
    const response = await fetchWithAuth('/api/products', {
      method: 'POST',
      body: JSON.stringify(data),
    });
    if (!response.ok) {
      throw new Error('Error al crear producto.');
    }
  },

  async updateProduct(id: string, data: UpdateProductRequest): Promise<void> {
    const response = await fetchWithAuth(`/api/products/${id}`, {
      method: 'PUT',
      body: JSON.stringify(data),
    });
    if (!response.ok) {
      throw new Error('Error al actualizar producto.');
    }
  },

  async deleteProduct(id: string): Promise<void> {
    const response = await fetchWithAuth(`/api/products/${id}`, {
      method: 'DELETE',
    });
    if (!response.ok) {
      throw new Error('Error al eliminar producto.');
    }
  },
};