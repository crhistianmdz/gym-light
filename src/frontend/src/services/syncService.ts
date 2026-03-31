import { db, type SyncEventType, type SyncQueueItem, type ErrorQueueItem } from '@/db/gymflow.db'
import { fetchWithAuth } from '@/services/httpClient'

const MAX_RETRIES = 3
const SYNC_INTERVAL_MS = 5 * 60 * 1000 // 5 minutes
const SYNC_LOCK_KEY = 'syncLock' // This key prevents concurrent sync processes
const REQUEST_TIMEOUT_MS = 10000 // 10 seconds

const ENDPOINT_MAP: Record<SyncEventType, { url: (payload: Record<string, any>) => string; method: string }> = {
  MemberUpdate: { url: (payload: Record<string, any>) => `/api/members/${payload['memberId']}/cancel`, method: 'POST' },
  CheckIn: { url: () => '/api/checkin', method: 'POST' },
  Sale: { url: () => '/api/sales', method: 'POST' },
  SaleCancel: { url: () => '/api/sales', method: 'DELETE' },
  HealthUpdate: { url: () => '/api/members/measurements', method: 'POST' },
  WorkoutLogCreate: { url: () => '/api/workout-logs', method: 'POST' },
}

export class SyncService {
  private syncInterval: number | null = null

  startSync(): void {
    window.addEventListener('online', () => this.processQueue())
    this.syncInterval = window.setInterval(() => this.processQueue(), SYNC_INTERVAL_MS)
  }

  stopSync(): void {
    window.removeEventListener('online', () => this.processQueue())
    if (this.syncInterval !== null) {
      clearInterval(this.syncInterval)
      this.syncInterval = null
    }
  }

  async processQueue(): Promise<void> {
    if (!navigator.onLine) return

    const syncLock = await db.metadata.get(SYNC_LOCK_KEY)
    if (syncLock?.value === 'true') return

    await db.metadata.put({ key: SYNC_LOCK_KEY, value: 'true' })

    try {
      const queueItems = await db.sync_queue.orderBy('timestamp').toArray()

      for (const item of queueItems) {
        try {
          const endpoint = ENDPOINT_MAP[item.type]
          const options: RequestInit = {
            method: endpoint.method,
            headers: { 'X-Client-Guid': item.guid },
            body: item.payload ? JSON.stringify(item.payload) : undefined,
          }

          const url = endpoint.url(JSON.parse(item.payload))
          const response = await this.fetchWithAbort(url, options)

          if (response.ok) {
            const data = await response.json()
            await this.updateLocalCache(item.type, data)
            await db.sync_queue.delete(item.guid)
          } else if (response.status === 401) {
            window.dispatchEvent(new CustomEvent('sync:auth-required'))
            break
          } else {
            await this.handleFailure(item, response.status)
          }
        } catch (error) {
          await this.handleFailure(item, error.message)
        }
      }
    } finally {
      await db.metadata.put({ key: SYNC_LOCK_KEY, value: 'false' })
      window.dispatchEvent(new CustomEvent('sync:completed'))
    }
  }

  private async handleFailure(item: SyncQueueItem, error: string | number): Promise<void> {
    const updatedItem: SyncQueueItem = { ...item, retryCount: item.retryCount + 1 }
    if (updatedItem.retryCount >= MAX_RETRIES) {
      const errorItem: ErrorQueueItem = {
        guid: updatedItem.guid,
        type: updatedItem.type,
        payload: updatedItem.payload,
        timestamp: updatedItem.timestamp,
        retryCount: updatedItem.retryCount,
        lastError: String(error),
        failedAt: Date.now(),
      }
      await db.error_queue.add(errorItem)
      await db.sync_queue.delete(item.guid)
    } else {
      await db.sync_queue.put(updatedItem)
    }

    window.dispatchEvent(
      new CustomEvent('sync:item-failed', { detail: { guid: item.guid, type: item.type, error } })
    )
  }

  private async fetchWithAbort(url: string, options: RequestInit): Promise<Response> {
    const controller = new AbortController()
    const timeoutId = setTimeout(() => controller.abort(), REQUEST_TIMEOUT_MS)
    try {
      return await fetchWithAuth(url, { ...options, signal: controller.signal })
    } finally {
      clearTimeout(timeoutId)
    }
  }

  async updateLocalCache(type: SyncEventType, data: Record<string, unknown>): Promise<void> {
    if (type === 'MemberUpdate') {
      const update = data as { memberId: string; status: string; autoRenewEnabled: boolean; cancelledAt?: string }
      await db.users.where('id').equals(update.memberId).modify({
        status: update.status,
        autoRenewEnabled: update.autoRenewEnabled,
        cancelledAt: update.cancelledAt ?? null,
        syncStatus: 'synced',
      })
    } else if (type === 'CheckIn') {
      const { memberId, lastCheckIn, status } = data as { memberId: string; lastCheckIn: string; status: string }
      await db.users.update(memberId, { lastCheckIn, status })
    } else if (type === 'Sale') {
      const sale = data as {
        id: string
        clientGuid: string
        status: string
        total: number
        timestamp: string
        lines: Array<{ productId: string; quantity: number; unitPrice: number; subtotal: number; productName: string }>
      }
      await db.sales.put({
        id: sale.id,
        clientGuid: sale.clientGuid,
        lines: sale.lines.map((l) => ({
          productId: l.productId,
          productName: l.productName,
          quantity: l.quantity,
          unitPrice: l.unitPrice,
          subtotal: l.subtotal,
        })),
        total: sale.total,
        status: 'synced',
        timestamp: new Date(sale.timestamp).getTime(),
        isOffline: false,
        retryCount: 0,
      })
      // Actualizar stock local de cada producto tras confirmación del servidor
      for (const line of sale.lines) {
        await db.products
          .where('id')
          .equals(line.productId)
          .modify((product) => {
            product.stock = Math.max(0, product.stock - line.quantity)
          })
      }
    } else if (type === 'HealthUpdate') {
      const m = data as { clientGuid: string; id: number }
      await db.measurements.where('clientGuid').equals(m.clientGuid).modify({
        syncStatus: 'synced',
        id: m.id,
      })
    } else if (type === 'WorkoutLogCreate') {
      const log = data as { clientGuid: string; id: string }
      await db.workout_logs.where('clientGuid').equals(log.clientGuid).modify({
        syncStatus: 'synced',
        id: log.id,
      })
    }
  }

  async getPendingCount(): Promise<number> {
    return db.sync_queue.count()
  }

  async getErrorCount(): Promise<number> {
    return db.error_queue.count()
  }
}

  async retryFromErrorQueue(guid: string): Promise<void> {
    const item = await db.error_queue.get(guid);
    if (!item) return;
    // Mover de error_queue a sync_queue con retryCount reseteado
    await db.sync_queue.put({
      guid: item.guid,
      type: item.type,
      payload: item.payload,
      timestamp: Date.now(),
      isOffline: true,
      retryCount: 0,
    });
    await db.error_queue.delete(guid);
  }

  async discardFromErrorQueue(guid: string): Promise<void> {
    await db.error_queue.delete(guid);
  }

export const syncService = new SyncService()
