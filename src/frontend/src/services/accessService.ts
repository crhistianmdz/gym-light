import type { MemberStatus } from '@/db/gymflow.db'
import { db } from '@/db/gymflow.db'

// ─── Tipos ────────────────────────────────────────────────────────────────────

export interface AccessResult {
  allowed: boolean
  member: {
    id: string
    fullName: string
    photoWebP: string
    status: MemberStatus
    membershipEndDate: string
  } | null
  denialReason: string | null
  /** 'online' = validado contra el servidor | 'offline' = validado contra IndexedDB */
  source: 'online' | 'offline'
}

// ─── Constantes ───────────────────────────────────────────────────────────────

const API_BASE = import.meta.env.VITE_API_BASE_URL ?? ''
const NETWORK_TIMEOUT_MS = 2000 // RFC §3: timeout >2s activa fallback offline

// ─── Servicio ─────────────────────────────────────────────────────────────────

/**
 * Valida el acceso de un socio al gimnasio.
 *
 * Estrategia Network-First con fallback offline (RFC §3):
 *   1. Intentar POST /api/access/checkin con timeout de 2s
 *   2. Si éxito → rehidratar store `users` en IndexedDB
 *   3. Si falla → consultar IndexedDB y encolar en sync_queue
 *
 * HU-01 criterios:
 *   CA-1: Fallback a IndexedDB si API falla o timeout >2s
 *   CA-2: Retorna photoWebP para verificación visual
 *   CA-3: Encola AccessLog en sync_queue con ClientGuid
 */
export async function checkInMember(
  performedByUserId?: string,
  memberId: string,
  performedByUserId: string,
): Promise<AccessResult> {
  if (!performedByUserId || performedByUserId === '00000000-0000-0000-0000-000000000000') {
    throw new Error('performedByUserId is required for check-in traceability');
  }

  const clientGuid = crypto.randomUUID() // PRD §4.4: UUID v4 por transacción

  try {
    const result = await fetchWithTimeout(
      `${API_BASE}/api/access/checkin`,
      {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'X-Client-Guid': clientGuid,
        },
        credentials: 'include', // JWT en HttpOnly Cookie (RFC §7)
        body: JSON.stringify({ memberId, clientGuid, performedByUserId }),
      },
      NETWORK_TIMEOUT_MS,
    )

    if (result.ok || result.status === 403) {
      const data = await result.json()
      const isAllowed = result.ok && data.allowed === true

      // Re-hidratación de IndexedDB (RFC §3 — Modo Online)
      if (data.member) {
        await db.users.put({
          id: data.member.id,
          fullName: data.member.fullName,
          photoWebP: data.member.photoWebPUrl,
          status: data.member.status,
          membershipEndDate: data.member.membershipEndDate,
        })
      }

      return {
        allowed: isAllowed,
        member: data.member
          ? {
              id: data.member.id,
              fullName: data.member.fullName,
              photoWebP: data.member.photoWebPUrl,
              status: data.member.status,
              membershipEndDate: data.member.membershipEndDate,
            }
          : null,
        denialReason: data.denialReason ?? null,
        source: 'online',
      }
    }

    throw new Error(`Respuesta inesperada del servidor: ${result.status}`)
  } catch {
    // ── Fallback offline (RFC §3 — Modo Offline) ──────────────────────────
    return await validateOffline(memberId, clientGuid, performedByUserId)
  }
}

// ─── Helpers privados ─────────────────────────────────────────────────────────

/**
 * Valida contra IndexedDB y encola el AccessLog en sync_queue.
 * HU-01 CA-1 y CA-3.
 */
async function validateOffline(
  memberId: string,
  clientGuid: string,
  performedByUserId: string,
): Promise<AccessResult> {
  const member = await db.users.get(memberId)

  if (!member) {
    return {
      allowed: false,
      member: null,
      denialReason: 'Socio no encontrado en caché local.',
      source: 'offline',
    }
  }

  const today = new Date().toISOString().slice(0, 10)
  const allowed =
    member.status === 'Active' && member.membershipEndDate >= today

  const denialReason = !allowed
    ? member.status === 'Frozen'
      ? 'La membresía está congelada.'
      : 'La membresía está vencida.'
    : null

  // Encolar en sync_queue (HU-01 CA-3, PRD §4.4)
  await db.sync_queue.add({
    guid: clientGuid,
    type: 'CheckIn',
    payload: JSON.stringify({
      memberId,
      clientGuid,
      performedByUserId,
      wasAllowed: allowed,
      isOffline: true,
    }),
    timestamp: Date.now(),
    isOffline: true,
    retryCount: 0,
  })

  return {
    allowed,
    member: {
      id: member.id,
      fullName: member.fullName,
      photoWebP: member.photoWebP,
      status: member.status,
      membershipEndDate: member.membershipEndDate,
    },
    denialReason,
    source: 'offline',
  }
}

/** Fetch con AbortController para implementar el timeout de 2s (RFC §3). */
async function fetchWithTimeout(
  url: string,
  options: RequestInit,
  timeoutMs: number,
): Promise<Response> {
  const controller = new AbortController()
  const timer = setTimeout(() => controller.abort(), timeoutMs)

  try {
    return await fetch(url, { ...options, signal: controller.signal })
  } finally {
    clearTimeout(timer)
  }
}
