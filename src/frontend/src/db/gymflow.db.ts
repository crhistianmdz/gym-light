import Dexie, { type Table } from 'dexie';

// —— Core types ——
export type MemberStatus = 'Active' | 'Frozen' | 'Expired';
export type SyncEventType =
  | 'CheckIn'
  | 'Sale'
  | 'SaleCancel'
  | 'HealthUpdate'
  | 'WorkoutLogCreate'
  | 'MemberUpdate';

export interface LocalMember {
  id: string;
  fullName: string;
  photoWebP?: string;
  status: MemberStatus;
  membershipEndDate: string;
}

export interface SyncQueueItem {
  guid: string;
  type: SyncEventType;
  payload: string;
  timestamp: number;
  isOffline: boolean;
  retryCount: number;
}

export interface PaymentLocal {
  id: string;
  memberId?: string;
  amount: number;
  category: 0 | 1; // 0 = Membership, 1 = POS
  timestamp: number; // epoch ms
  syncStatus: 'synced' | 'pending';
  clientGuid: string;
  notes?: string;
  saleId?: string;
  createdByUserId: string;
}

// Type used to track failed sync attempts
export interface ErrorQueueItem {
  guid: string;
  type: SyncEventType;
  payload: string;
  retryCount: number;
  timestamp: number;
  lastError?: string;
  failedAt?: number;
}

class GymflowDB extends Dexie {
  users!: Table<LocalMember, string>;
  sync_queue!: Table<SyncQueueItem, string>;
  error_queue!: Table<ErrorQueueItem, string>;
  metadata!: Table<any, string>;
  products!: Table<any, string>;
  sales!: Table<any, string>;
  payments!: Table<PaymentLocal, string>;
  measurements!: Table<any, string>;
  exercise_catalog!: Table<any, string>;
  routines!: Table<any, string>;
  routine_assignments!: Table<any, string>;
  workout_logs!: Table<any, string>;

  constructor() {
    super('GymflowDB');

    this.version(6).stores({
      users: 'id,fullName,status',
      sync_queue: 'guid,type,timestamp,isOffline',
      error_queue: '++id,timestamp',
      metadata: 'key',
      products: 'id,name,stock',
      sales: 'id,timestamp,product',
      payments: 'id, memberId, category, timestamp, syncStatus',
      measurements: 'id,memberId,date',
      exercise_catalog: 'id,name,type',
      routines: 'id,name,createdBy',
      routine_assignments: 'id,memberId,routineId',
      workout_logs: 'id,exercise,date'
    });
  }
}

export const db = new GymflowDB();
