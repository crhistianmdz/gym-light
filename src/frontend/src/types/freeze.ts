/**
 * Tipos para la lógica de congelamiento de membresías.
 * HU-07: Lógica de Congelamiento de Membresía.
 */

/** Evento de congelamiento retornado por la API. */
export interface MembershipFreeze {
  id: string
  memberId: string
  startDate: string        // ISO date 'YYYY-MM-DD'
  endDate: string          // ISO date 'YYYY-MM-DD'
  durationDays: number
  createdByUserId: string
  createdAt: string        // ISO datetime
}

/** Payload para el endpoint POST /api/members/{id}/freeze */
export interface FreezeMembershipRequest {
  startDate: string        // 'YYYY-MM-DD'
  endDate: string          // 'YYYY-MM-DD'
}

/** Respuesta al descongelar (MemberDto del backend) */
export interface UnfreezeResponse {
  id: string
  fullName: string
  photoWebPUrl: string
  status: 'Active' | 'Frozen' | 'Expired'
  membershipEndDate: string
}
