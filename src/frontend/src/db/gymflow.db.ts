import Dexie, { type EntityTable } from 'dexie';

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

class GymflowDB extends Dexie {
  users!: EntityTable<LocalMember, 'id'>;
  sync_queue!: EntityTable<SyncQueueItem, 'guid'>;
  error_queue!: EntityTable<any, 'id'>;
  metadata!: EntityTable<any, 'key'>;
  products!: EntityTable<any, 'id'>;
  sales!: EntityTable<any, 'id'>;
  payments!: EntityTable<PaymentLocal, 'id'>;
  measurements!: EntityTable<any, 'id'>;
  exercise_catalog!: EntityTable<any, 'id'>;
  routines!: EntityTable<any, 'id'>;
  routine_assignments!: EntityTable<any, 'id'>;
  workout_logs!: EntityTable<any, 'id'>;

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