import type { MemberStatus } from '@/db/gymflow.db'

/** Socio tal como lo expone la API (MemberDto del backend) */
export interface MemberDto {
  id: string
  fullName: string
  photoWebPUrl: string
  status: MemberStatus
  membershipEndDate: string
}

/** Alias para compatibilidad — preferir MemberDto */
export type Member = MemberDto

/** Socio cacheado en IndexedDB */
export interface LocalMember {
  id: string
  fullName: string
  /** Data URI WebP o URL relativa — lo que esté cacheado localmente */
  photoWebP: string
  status: MemberStatus
  membershipEndDate: string
}
