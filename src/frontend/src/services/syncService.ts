import { db, type SyncEventType, type SyncQueueItem, type ErrorQueueItem } from '@/db/gymflow.db'
import { fetchWithAuth } from '@/services/httpClient'

const MAX_RETRIES = 3
const SYNC_INTERVAL_MS = 5 * 60 * 1000 // 5 minutes
const SYNC_LOCK_KEY = 'syncLock' // This key prevents concurrent sync processes
const REQUEST_TIMEOUT_MS = 10000 // 10 seconds

const ENDPOINT_MAP: Record<SyncEventType, { url: string; method: string }> = {
  CheckIn: { url: '/api/checkin', method: 'POST' },
  Sale: { url: '/api/sales', method: 'POST' },
  SaleCancel: { url: '/api/sales', method: 'DELETE' },
  MemberUpdate: { url: '/api/members', method: 'PUT' },
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
            body: item.type !== 'SaleCancel' ? item.payload : undefined,
          }

          const response = await this.fetchWithAbort(endpoint.url, options)

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

  async moveToErrorQueue(item: SyncQueueItem, error: string): Promise<void> {
    const errorItem: ErrorQueueItem = {
      guid: item.guid,
      type: item.type,
      payload: item.payload,
      timestamp: item.timestamp,
      retryCount: item.retryCount,
      lastError: error,
      failedAt: Date.now(),
    }
    await db.error_queue.add(errorItem)
    await db.sync_queue.delete(item.guid)
  }

  async retryFromErrorQueue(guid: string): Promise<void> {
    const item = await db.error_queue.get(guid)
    if (!item) return

    await db.sync_queue.add({ ...item, retryCount: 0 })
    await db.error_queue.delete(guid)
  }

  async discardFromErrorQueue(guid: string): Promise<void> {
    await db.error_queue.delete(guid)
  }

  async updateLocalCache(type: SyncEventType, data: Record<string, unknown>): Promise<void> {
    if (type === 'Sale') {
      const sale = data as { id: string; clientGuid: string; status: string; total: number; timestamp: string; lines: Array<{ productId: string; quantity: number; unitPrice: number; subtotal: number; productName: string }> }
      await db.sales.put({
        id: sale.id,
        clientGuid: sale.clientGuid,
        lines: sale.lines.map(l => ({
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
    }
  }

  async getPendingCount(): Promise<number> {
    return db.sync_queue.count()
  }

  async getErrorCount(): Promise<number> {
    return db.error_queue.count()
  }
}

export const syncService = new SyncService()