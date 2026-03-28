import { useState, useCallback } from 'react'
import { registerMember, type CreateMemberFormData } from '@/services/memberService'
import type { MemberDto } from '@/types/member'

type RegisterStatus = 'idle' | 'compressing' | 'uploading' | 'success' | 'error'

interface UseRegisterMemberReturn {
  register: (data: CreateMemberFormData) => Promise<void>
  status: RegisterStatus
  result: MemberDto | null
  loading: boolean
  error: string | null
  reset: () => void
}

/**
 * Hook que encapsula el flujo completo de registro de socio.
 *
 * Expone `status` granular para que la UI pueda mostrar
 * "Comprimiendo imagen..." vs "Guardando..." — UX más informativa.
 *
 * HU-02: la compresión y el upload están en memberService.ts.
 * Este hook solo gestiona el estado de UI.
 */
export function useRegisterMember(): UseRegisterMemberReturn {
  const [status, setStatus] = useState<RegisterStatus>('idle')
  const [result, setResult] = useState<MemberDto | null>(null)
  const [error, setError] = useState<string | null>(null)

  const register = useCallback(async (data: CreateMemberFormData) => {
    setStatus('compressing')
    setResult(null)
    setError(null)

    try {
      // La compresión es la parte más lenta — mostrar estado diferenciado
      setStatus('uploading')
      const member = await registerMember(data)
      setResult(member)
      setStatus('success')
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Error inesperado al registrar el socio.'
      setError(message)
      setStatus('error')
    }
  }, [])

  const reset = useCallback(() => {
    setStatus('idle')
    setResult(null)
    setError(null)
  }, [])

  return {
    register,
    status,
    result,
    loading: status === 'compressing' || status === 'uploading',
    error,
    reset,
  }
}
