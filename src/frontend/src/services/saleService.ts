import { db } from '@/db/gymflow.db';
import { fetchWithAuth } from '@/services/httpClient';

// Tipos mejorados para HU-04
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
export interface SaleLineResponse {
    id: string;
    productId: string;
    productName: string;
    quantity: number;
    unitPrice: number;
    subtotal: number;
}
export interface SaleResponse {
    id: string;
    clientGuid: string;
    performedByUserId: string;
    timestamp: string;
    status: string;
    total: number;
    lines: SaleLineResponse[];
    isOffline?: boolean;
}

// Servicio principal con funciones específicas
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
            return offlineProducts.map((product: ProductResponse) => ({
                ...product,
                isLowStock: product.stock <= product.initialStock * 0.2,
            }));
        }
    },

    async createSale(request: CreateSaleRequest): Promise<void> {
        // Eliminación de clientGuid interno, usar el recibido
// Eliminado: const clientGuid = crypto.randomUUID();
        try {
            const response = await fetchWithAuth('/api/sales', {
                method: 'POST',
                headers: { 'X-Client-Guid': request.clientGuid },
                body: JSON.stringify(request),
            });
            if (!response.ok) throw new Error("Couldn't create sale");
            await db.sales.put({
                ...request,
                timestamp: Date.now(),
                status: 'synced',
                isOffline: false
            });
        } catch (error) {
            await db.sync_queue.add({
                guid: request.clientGuid,
                type: 'Sale',
                payload: JSON.stringify(request),
                timestamp: Date.now(),
                retryCount: 0,
                isOffline: true,
            });
            await db.sales.put({
                ...request,
                timestamp: Date.now(),
                status: 'pending',
                isOffline: true
            });
        }
    },
  async getSales(page: number, pageSize: number): Promise<{ data: SaleResponse[]; total: number }> {
    try {
      const response = await fetchWithAuth(`/api/sales?page=${page}&pageSize=${pageSize}`);
      if (!response.ok) {
        throw new Error('Error al obtener ventas.');
      }
      const result = await response.json();
      return result;
    } catch (error) {
      console.warn('Error de red, cargando datos offline', error);
      const offlineSales = await db.sales
        .orderBy('timestamp')
        .reverse()
        .offset((page - 1) * pageSize)
        .limit(pageSize)
        .toArray();
      const total = await db.sales.count();
      return { data: offlineSales, total };
    }
  },

  async cancelSale(saleId: string): Promise<void> {
    try {
      const response = await fetchWithAuth(`/api/sales/${saleId}`, { method: 'DELETE' });
      if (!response.ok) {
        throw new Error('Error al cancelar venta.');
      }
      await db.sales.update(saleId, { status: 'cancelled' });
    } catch (error) {
      console.warn('Error de red al cancelar venta, encolando acción', error);
      await db.sync_queue.add({
        guid: crypto.randomUUID(), // Generar nuevo UUID
        type: 'SaleCancel',
        payload: JSON.stringify({ id: saleId }),
        timestamp: Date.now(),
        retryCount: 0,
        isOffline: true,
      });
      await db.sales.update(saleId, { status: 'cancelled' });
      throw new Error('OFFLINE_QUEUED');
    }
  },
};