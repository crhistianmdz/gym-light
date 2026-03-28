import { db } from '@/db/gymflow.db'
import { compressToWebP } from '@/services/imageService'
import type { MemberDto } from '@/types/member'

const API_BASE = import.meta.env.VITE_API_BASE_URL ?? ''

export interface CreateMemberFormData {
  fullName: string
  photo: File
  membershipEndDate: string // 'YYYY-MM-DD'
}

/**
 * Registra un nuevo socio en el sistema.
 *
 * Flujo HU-02:
 *   1. Comprimir imagen a WebP (CA-2) — imageService.compressToWebP()
 *   2. POST /api/members con el data URI WebP
 *   3. Si éxito → guardar en IndexedDB store `users` (disponible offline de inmediato)
 *   4. Retornar MemberDto
 *
 * Nota: el registro de socio siempre requiere conexión (no tiene fallback offline
 * porque crear un socio sin sincronizar sería un riesgo de integridad de datos).
 */
export async function registerMember(data: CreateMemberFormData): Promise<MemberDto> {
  // ── Paso 1: Comprimir a WebP (CA-2) ───────────────────────────────────────
  const photoWebPBase64 = await compressToWebP(data.photo)

  // ── Paso 2: Enviar al servidor ─────────────────────────────────────────────
  const response = await fetch(`${API_BASE}/api/members`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    credentials: 'include', // JWT en HttpOnly Cookie
    body: JSON.stringify({
      fullName: data.fullName,
      photoWebPBase64,
      membershipEndDate: data.membershipEndDate,
    }),
  })

  if (!response.ok) {
    const errorBody = await response.json().catch(() => null)
    const detail = errorBody?.detail ?? errorBody?.title ?? 'Error desconocido al registrar el socio.'
    throw new Error(detail)
  }

  const member: MemberDto = await response.json()

  // ── Paso 3: Hidratar IndexedDB (Autoridad del Servidor — PRD §4.3) ─────────
  // La foto se cachea como el data URI comprimido para verificación visual offline (HU-01 CA-2)
  await db.users.put({
    id: member.id,
    fullName: member.fullName,
    photoWebP: photoWebPBase64, // caché local del WebP para acceso offline
    status: member.status,
    membershipEndDate: member.membershipEndDate,
  })

  return member
}
