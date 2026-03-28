import { db } from '@/db/gymflow.db';
import { fetchWithAuth } from '@/services/httpClient';
import { crypto } from 'crypto';

// Required Types
export interface ProductResponse {
  id: string;
  sku?: string;
  name: string;
  description?: string;
  price: number;
  stock: number;
  initialStock: number;
  isLowStock: boolean;
}
export interface SaleLineRequest {
  productId: string;
  quantity: number;
}
export interface CreateSaleRequest {
  clientGuid: string;
  lines: SaleLineRequest[];
  performedByUserId: string;
}
export interface SaleResponse {
  id: string;
  clientGuid: string;
  performedByUserId: string;
  timestamp: string;
  status: string;
  total: number;
  lines: SaleLineResponse[];
}
export interface SaleLineResponse {
  id: string;
  productId: string;
  productName: string;
  quantity: number;
  unitPrice: number;
  subtotal: number;
}

/**
 * Helper: Identifies if stock is low.
 */
function isLowStock(stock: number, initialStock: number): boolean {
  return stock <= initialStock * 0.2;
}

export const saleService = {
  async getProducts(): Promise<ProductResponse[]> {
    try {
      const response = await fetchWithAuth('/api/products');
      if (!response.ok) throw new Error('Failed to fetch products.');
      const products: ProductResponse[] = await response.json();
      products.forEach(async product => await db.products.put(product));
      return products;
    } catch (error) {
      const offlineProducts = await db.products.toArray();
      return offlineProducts.map(product => ({
        ...product,
        isLowStock: isLowStock(product.stock, product.initialStock),
      }));
    }
  },
  async createSale(request: CreateSaleRequest): Promise<void> {
    const clientGuid = crypto.randomUUID();
    try {
      const response = await fetchWithAuth('/api/sales', {
        method: 'POST',
        headers: { 'X-Client-Guid': clientGuid },
        body: JSON.stringify(request)
      });
      if (!response.ok) throw new Error("Couldn’t create sale");
      await db.sales.put({
        ...request,
        timestamp: Date.now(),
        status: 'synced',
        isOffline:false
      });
    }}}>REQUEST_PENDING...Verify typo saf">Awesome,