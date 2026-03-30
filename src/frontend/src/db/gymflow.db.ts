import Dexie, { type EntityTable } from 'dexie';

// —— Preserved existing types from the file ——
export type MemberStatus = 'Active' | 'Frozen' | 'Expired';
export type SyncEventType = 'CheckIn' | 'Sale' | 'HealthUpdate' | 'SaleCancel';

export interface SyncQueueItem {
  guid: string;
  type: SyncEventType;
  payload: string;
  timestamp: number;
  isOffline: boolean;
  retryCount: number;
}

// —— New types for HU-03 ——
export interface ProductLocal {
  id: string;
  sku?: string;
  name: string;
  description?: string;
  price: number;
  stock: number;
  initialStock: number;
  updatedAt: number;
}

export interface SaleLineLocal {
  productId: string;
  productName: string;
  quantity: number;
  unitPrice: number;
  subtotal: number;
}

export interface SaleLocal {
  id: string;
  clientGuid: string;
  lines: SaleLineLocal[];
  total: number;
  status: 'pending' | 'synced' | 'cancelled';
  timestamp: number;
  isOffline: boolean;
  retryCount: number;
}

export interface LocalMetadata {
  key: string;
  value: string;
}

export interface ErrorQueueItem {
  guid: string;
  type: SyncEventType;
  payload: string;
  timestamp: number;
  retryCount: number;
  lastError: string;
  failedAt: number;
}

// Added for HU-09 Anthropometric
export interface MeasurementLocal {
  id?: number;
  memberId: string;
  recordedById: string;
  recordedAt: string; // ISO string
  weightKg: number;
  bodyFatPct: number;
  chestCm: number;
  waistCm: number;
  hipCm: number;
  armCm: number;
  legCm: number;
  unitSystem: 'metric' | 'imperial';
  notes?: string;
  clientGuid: string;
  syncStatus: 'pending' | 'synced' | 'error';
}

class GymFlowDatabase extends Dexie {
  users!: EntityTable<LocalMember, 'id'>;
  sync_queue!: EntityTable<SyncQueueItem, 'guid'>;
  metadata!: EntityTable<LocalMetadata, 'key'>;
  products!: EntityTable<ProductLocal, 'id'>;
  sales!: EntityTable<SaleLocal, 'id'>;
  error_queue!: EntityTable<ErrorQueueItem, 'guid'>;

  // Added anthropometry
  measurements!: EntityTable<MeasurementLocal, 'id'>;

  constructor() {
    super('gymflow');

    this.version(4).stores({
      users: 'id, status, membershipEndDate',
      sync_queue: 'guid, type, timestamp, retryCount',
      metadata: 'key',
      products: 'id, name, sku, stock',
      sales: 'id, clientGuid, status, timestamp',
      error_queue: 'guid, type, timestamp, retryCount',

      // New measurements schema
      measurements: '++id, memberId, clientGuid, recordedAt, syncStatus'
    });
  }
}

export const db = new GymFlowDatabase();

export async function initDatabase(): Promise<void> {
  if (navigator.storage?.persist) {
    const isPersisted = await navigator.storage.persist();
    if (!isPersisted) {
      console.warn('[GymFlow DB] Storage persistence no fue otorgada por el navegador.');
    }
  }

  await db.open();
}