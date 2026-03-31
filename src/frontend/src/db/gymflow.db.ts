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

// —— HU-03: POS ——
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

// —— HU-09: Medidas antropométricas ——
export interface MeasurementLocal {
  id?: number;
  memberId: string;
  recordedById: string;
  recordedAt: string;
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

// —— HU-11: Rutinas Digitales ——
export interface ExerciseCatalogLocal {
  id: string;
  name: string;
  description?: string;
  mediaUrl?: string;
  isCustom: boolean;
}

export interface RoutineExerciseLocal {
  id: string;
  exerciseCatalogId?: string;
  catalogExerciseName?: string;
  customName?: string;
  order: number;
  sets: number;
  reps: number;
  notes?: string;
}

export interface RoutineLocal {
  id: string;
  name: string;
  description?: string;
  isPublic: boolean;
  createdByUserId: string;
  updatedAt: string;
  exercises: RoutineExerciseLocal[];
}

export interface RoutineAssignmentLocal {
  id: string;
  routineId: string;
  routineName: string;
  memberId: string;
  assignedByUserId: string;
  assignedAt: string;
  isActive: boolean;
  routine?: RoutineLocal;
}

export interface WorkoutEntryLocal {
  routineExerciseId: string;
  exerciseName: string;
  sets: number;
  reps: number;
  completed: boolean;
  completedAt?: string;
  notes?: string;
}

export interface WorkoutLogLocal {
  id: string;
  assignmentId: string;
  memberId: string;
  sessionDate: string;
  clientGuid: string;
  createdAt: string;
  syncStatus: 'synced' | 'pending' | 'error';
  entries: WorkoutEntryLocal[];
}

// —— Database class ——
class GymFlowDatabase extends Dexie {
  users!: EntityTable<LocalMember, 'id'>;
  sync_queue!: EntityTable<SyncQueueItem, 'guid'>;
  metadata!: EntityTable<LocalMetadata, 'key'>;
  products!: EntityTable<ProductLocal, 'id'>;
  sales!: EntityTable<SaleLocal, 'id'>;
  error_queue!: EntityTable<ErrorQueueItem, 'guid'>;
  measurements!: EntityTable<MeasurementLocal, 'id'>;
  exercise_catalog!: EntityTable<ExerciseCatalogLocal, 'id'>;
  routines!: EntityTable<RoutineLocal, 'id'>;
  routine_assignments!: EntityTable<RoutineAssignmentLocal, 'id'>;
  workout_logs!: EntityTable<WorkoutLogLocal, 'id'>;

  constructor() {
    super('gymflow');

    this.version(1).stores({
      users: 'id, status, membershipEndDate',
      sync_queue: 'guid, type, timestamp',
      metadata: 'key',
    });

    this.version(2).stores({
      users: 'id, status, membershipEndDate',
      sync_queue: 'guid, type, timestamp',
      metadata: 'key',
      products: 'id, name, sku, stock',
      sales: 'id, clientGuid, status, timestamp',
    });

    this.version(3).stores({
      users: 'id, status, membershipEndDate',
      sync_queue: 'guid, type, timestamp',
      metadata: 'key',
      products: 'id, name, sku, stock',
      sales: 'id, clientGuid, status, timestamp',
      error_queue: 'guid, type, timestamp, retryCount',
    });

    this.version(4).stores({
      users: 'id, status, membershipEndDate',
      sync_queue: 'guid, type, timestamp',
      metadata: 'key',
      products: 'id, name, sku, stock',
      sales: 'id, clientGuid, status, timestamp',
      error_queue: 'guid, type, timestamp, retryCount',
      measurements: '++id, memberId, clientGuid, syncStatus',
    });

    // HU-11: Rutinas Digitales
    this.version(5).stores({
      users: 'id, status, membershipEndDate',
      sync_queue: 'guid, type, timestamp',
      metadata: 'key',
      products: 'id, name, sku, stock',
      sales: 'id, clientGuid, status, timestamp',
      error_queue: 'guid, type, timestamp, retryCount',
      measurements: '++id, memberId, clientGuid, syncStatus',
      exercise_catalog: 'id, name, isCustom',
      routines: 'id, createdByUserId, isPublic, updatedAt',
      routine_assignments: 'id, routineId, memberId, assignedAt',
      workout_logs: 'id, assignmentId, memberId, clientGuid, syncStatus',
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
