/**
 * freezeService — lógica de red para HU-07: Congelamiento de Membresía.
 *
 * Usa fetchWithAuth para manejar JWT automáticamente (refresh + retry).
 * Solo Admin y Owner pueden invocar estas funciones (validado también en el backend).
 */

import { fetchWithAuth } from '@/services/httpClient'
import { db } from '@/db/gymflow.db'
import type {
  MembershipFreeze,
  FreezeMembershipRequest,
  UnfreezeResponse,
} from '@/types/freeze'

const API_BASE = import.meta.env.VITE_API_BASE_URL ?? ''

/**
 * Congela la membresía de un socio.
 *
 * POST /api/members/{memberId}/freeze
 *
 * HU-07 Reglas aplicadas en el servidor:
 *   - Mínimo 7 días.
 *   - Máximo 4 congelamientos por año calendario.
 *   - Status cambia a Frozen, EndDate se extiende.
 *
 * Post-éxito: actualiza el caché local (IndexedDB) con status 'Frozen'.
 *
 * @throws Error si la validación falla o el servidor responde con error.
 */
export async function freezeMember(
  memberId: string,
  request: FreezeMembershipRequest,
): Promise<MembershipFreeze> {
  const res = await fetchWithAuth(`${API_BASE}/api/members/${memberId}/freeze`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
  })

  if (!res.ok) {
    const errorBody = await res.json().catch(() => null)
    const detail = errorBody?.detail ?? errorBody?.title ?? 'Error al congelar la membresía.'
    throw new Error(detail)
  }

  const freeze: MembershipFreeze = await res.json()

  // Actualizar caché local — servidor autoritativo (PRD §4.3 / RFC §4)
  await db.users.where('id').equals(memberId).modify({ status: 'Frozen' })

  return freeze
}

/**
 * Descongela la membresía de un socio.
 *
 * DELETE /api/members/{memberId}/freeze
 *
 * Post-éxito: actualiza el caché local con status 'Active'.
 *
 * @throws Error si el socio no está Frozen o hay error en el servidor.
 */
export async function unfreezeMember(memberId: string): Promise<UnfreezeResponse> {
  const res = await fetchWithAuth(`${API_BASE}/api/members/${memberId}/freeze`, {
    method: 'DELETE',
  })

  if (!res.ok) {
    const errorBody = await res.json().catch(() => null)
    const detail = errorBody?.detail ?? errorBody?.title ?? 'Error al descongelar la membresía.'
    throw new Error(detail)
  }

  const member: UnfreezeResponse = await res.json()

  // Actualizar caché local — servidor autoritativo
  await db.users.where('id').equals(memberId).modify({
    status: member.status,
    membershipEndDate: member.membershipEndDate,
  })

  return member
}

/**
 * Retorna el historial de congelamientos de un socio.
 *
 * GET /api/members/{memberId}/freezes
 *
 * @throws Error si el socio no existe o hay error de red.
 */
export async function getFreezeHistory(memberId: string): Promise<MembershipFreeze[]> {
  const res = await fetchWithAuth(`${API_BASE}/api/members/${memberId}/freezes`)

  if (!res.ok) {
    const errorBody = await res.json().catch(() => null)
    const detail = errorBody?.detail ?? errorBody?.title ?? 'Error al obtener el historial de congelamientos.'
    throw new Error(detail)
  }

  return res.json()
}
