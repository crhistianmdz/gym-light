/** Datos del formulario de registro de socio */
export interface MemberFormData {
  fullName: string
  photo: File | null
  membershipEndDate: string // 'YYYY-MM-DD'
}

/** Estado de validación por campo */
export interface MemberFormErrors {
  fullName?: string
  photo?: string
  membershipEndDate?: string
}
