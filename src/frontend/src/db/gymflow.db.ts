import Dexie, { type EntityTable } from 'dexie'

// ─── Tipos ────────────────────────────────────────────────────────────────────

export type MemberStatus = 'Active' | 'Frozen' | 'Expired'

export type SyncEventType = 'CheckIn' | 'Sale' | 'HealthUpdate'

export interface LocalMember {
  id: string
  fullName: string
  /** URL al archivo WebP cacheado localmente (CA-2: verificación visual) */
  photoWebP: string
  status: MemberStatus
  membershipEndDate: string // ISO date string 'YYYY-MM-DD'
}

export interface SyncQueueItem {
  /** UUID v4 generado en cliente — garantiza idempotencia (PRD §4.4) */
  guid: string
  type: SyncEventType
  payload: string // JSON serializado
  timestamp: number // Unix ms
  isOffline: boolean
  /** Intentos fallidos — al llegar a 3 pasa a bandeja de errores (RFC §6) */
  retryCount: number
}

export interface LocalMetadata {
  key: string
  value: string
}

// ─── Instancia Dexie ──────────────────────────────────────────────────────────

/**
 * Base de datos local de GymFlow Lite.
 * Esquema definido en RFC 001 §2.1.
 * PROHIBIDO usar localStorage para estos datos (AGENTS.md §7).
 */
class GymFlowDatabase extends Dexie {
  users!: EntityTable<LocalMember, 'id'>
  sync_queue!: EntityTable<SyncQueueItem, 'guid'>
  metadata!: EntityTable<LocalMetadata, 'key'>

  constructor() {
    super('gymflow')

    this.version(1).stores({
      users: 'id, status, membershipEndDate',
      sync_queue: 'guid, type, timestamp, retryCount',
      metadata: 'key',
    })
  }
}

export const db = new GymFlowDatabase()

// ─── Inicialización ───────────────────────────────────────────────────────────

/**
 * Solicita persistencia del storage para proteger IndexedDB de limpiezas
 * automáticas del navegador (HU-05 CA-2, RFC §7).
 */
export async function initDatabase(): Promise<void> {
  if (navigator.storage?.persist) {
    const isPersisted = await navigator.storage.persist()
    if (!isPersisted) {
      console.warn('[GymFlow DB] Storage persistence no fue otorgada por el navegador.')
    }
  }

  await db.open()
}
