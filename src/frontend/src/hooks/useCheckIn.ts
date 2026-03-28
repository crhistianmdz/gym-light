import { useState, useCallback } from 'react'
import { checkInMember, type AccessResult } from '@/services/accessService'

type CheckInStatus = 'idle' | 'loading' | 'allowed' | 'denied' | 'error'

interface UseCheckInReturn {
  checkIn: (memberId: string, performedByUserId: string) => Promise<void>
  status: CheckInStatus
  result: AccessResult | null
  loading: boolean
  error: string | null
  reset: () => void
}

/**
 * Hook que encapsula el flujo completo de check-in.
 * Maneja los 3 estados posibles: online permitido, offline permitido, denegado.
 *
 * HU-01: Toda la lógica de red y fallback está en accessService.ts.
 * Este hook solo gestiona el estado de UI.
 */
export function useCheckIn(): UseCheckInReturn {
  const [status, setStatus] = useState<CheckInStatus>('idle')
  const [result, setResult] = useState<AccessResult | null>(null)
  const [error, setError] = useState<string | null>(null)

  const checkIn = useCallback(
    async (memberId: string, performedByUserId: string) => {
      setStatus('loading')
      setResult(null)
      setError(null)

      try {
        const accessResult = await checkInMember(memberId, performedByUserId)
        setResult(accessResult)
        setStatus(accessResult.allowed ? 'allowed' : 'denied')
      } catch (err) {
        const message = err instanceof Error ? err.message : 'Error inesperado.'
        setError(message)
        setStatus('error')
      }
    },
    [],
  )

  const reset = useCallback(() => {
    setStatus('idle')
    setResult(null)
    setError(null)
  }, [])

  return {
    checkIn,
    status,
    result,
    loading: status === 'loading',
    error,
    reset,
  }
}
