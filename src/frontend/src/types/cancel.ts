/**
 * Tipos HU-08: Cancel Membership.
 */

/** Request para cancelar membresía */
export interface CancelMembershipRequest {
  clientGuid: string
}

/**
 * DTO extendido para resultados del servidor tras cancelar.
 */
export interface CancelMembershipResult {
  id: string
  fullName: string
  photoWebPUrl: string
  status: 'Active' | 'Cancelled' | 'Expired'
  autoRenewEnabled: boolean
  membershipEndDate: string
  cancelledAt?: string
}