import { fetchWithAuth } from '@/services/httpClient'
import { db } from '@/db/gymflow.db'

import type { CancelMembershipRequest, CancelMembershipResult } from '@/types/cancel'

const API_BASE = import.meta.env.VITE_API_BASE_URL ?? ''

/**
 * Lógica para cancelar la membresía de un usuario.
 * En caso de falla, encola la acción en IndexedDB.sync_queue.
 */
export async function cancelMembership(memberId: string): Promise<CancelMembershipResult> {
  const clientGuid = crypto.randomUUID()

  try {
    const res = await fetchWithAuth(`${API_BASE}/api/members/${memberId}/cancel`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ clientGuid } as CancelMembershipRequest),
    })

    if (!res.ok) {
      const errorBody = await res.json().catch(() => null)
      throw new Error(errorBody?.detail ?? 'Error al cancelar la membresía.')
    }

    const result: CancelMembershipResult = await res.json()

    // Actualiza IndexedDB desde el servidor (fuente autoritativa)
    await db.users.where('id').equals(memberId).modify({
      status: result.status,
      autoRenewEnabled: result.autoRenewEnabled,
      cancelledAt: result.cancelledAt,
    })

    return result
  } catch {
    // Encolar offline y actualizar IndexedDB de manera optimista
    await db.sync_queue.add({ guid: clientGuid, type: 'SaleCancel', timestamp: Date.now(), payload: JSON.stringify({ id: memberId, action: 'cancel', clientGuid }), isOffline: true, retryCount: 0 })
    await db.users.where('id').equals(memberId).modify({ autoRenewEnabled: false, status: 'Cancelled', cancelledAt: new Date().toISOString() })

    throw new Error('OFFLINE_QUEUED')
  }
}