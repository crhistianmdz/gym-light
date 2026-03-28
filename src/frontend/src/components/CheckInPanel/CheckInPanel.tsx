import { useState, type FormEvent } from 'react'
import { useCheckIn } from '@/hooks/useCheckIn'
import { useAuth } from '@/contexts/AuthContext'
import { MemberAccessCard } from './MemberAccessCard'
import { SyncStatusBadge } from './SyncStatusBadge'

interface CheckInPanelProps {
  /** ID del recepcionista autenticado (viene del contexto de auth) */
  currentUserId: string
}

/**
 * Panel principal de control de acceso para la recepción.
 *
 * HU-01: Validación de acceso offline.
 * Permite buscar un socio por ID y registrar su check-in,
 * con fallback automático a IndexedDB si no hay conexión.
 */
export function CheckInPanel({ currentUserId }: CheckInPanelProps) {
  const [memberId, setMemberId] = useState('')
  const { checkIn, status, result, loading, error, reset } = useCheckIn()

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    if (!memberId.trim()) return
    await checkIn(memberId.trim(), currentUserId)
  }

  const handleReset = () => {
    setMemberId('')
    reset()
  }

  return (
    <section aria-labelledby="checkin-title" style={{ padding: 24, maxWidth: 400 }}>
      <header style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <h2 id="checkin-title" style={{ margin: 0 }}>Control de Acceso</h2>
        <SyncStatusBadge />
      </header>

      <form onSubmit={handleSubmit} style={{ marginTop: 20 }}>
        <label htmlFor="member-id" style={{ display: 'block', marginBottom: 6, fontWeight: 500 }}>
          ID del Socio
        </label>
        <input
          id="member-id"
          type="text"
          value={memberId}
          onChange={(e) => setMemberId(e.target.value)}
          placeholder="Escaneá o ingresá el ID"
          disabled={loading}
          autoFocus
          style={{
            width: '100%',
            padding: '10px 12px',
            fontSize: 16,
            border: '1px solid #ccc',
            borderRadius: 6,
            boxSizing: 'border-box',
          }}
          aria-required="true"
        />

        <button
          type="submit"
          disabled={loading || !memberId.trim()}
          style={{
            marginTop: 12,
            width: '100%',
            padding: '12px 0',
            fontSize: 16,
            fontWeight: 700,
            backgroundColor: loading ? '#bbb' : '#1976d2',
            color: '#fff',
            border: 'none',
            borderRadius: 6,
            cursor: loading ? 'not-allowed' : 'pointer',
          }}
        >
          {loading ? 'Validando...' : 'Registrar Acceso'}
        </button>
      </form>

      {/* Resultado del check-in */}
      {result && result.member && (
        <div style={{ marginTop: 24 }}>
          <MemberAccessCard
            fullName={result.member.fullName}
            photoWebP={result.member.photoWebP}
            status={result.member.status}
            membershipEndDate={result.member.membershipEndDate}
            allowed={result.allowed}
            denialReason={result.denialReason}
            source={result.source}
          />
          <button
            onClick={handleReset}
            style={{
              marginTop: 12,
              padding: '8px 16px',
              fontSize: 14,
              cursor: 'pointer',
              borderRadius: 6,
              border: '1px solid #ccc',
              background: '#fff',
            }}
          >
            Nuevo check-in
          </button>
        </div>
      )}

      {/* Error del sistema */}
      {status === 'error' && error && (
        <div
          role="alert"
          style={{
            marginTop: 16,
            padding: 12,
            backgroundColor: '#fff3e0',
            border: '1px solid #ff9800',
            borderRadius: 6,
            fontSize: 14,
            color: '#e65100',
          }}
        >
          ⚠️ {error}
        </div>
      )}
    </section>
  )
}

/**
 * ConnectedCheckInPanel — versión que obtiene currentUserId desde useAuth().
 * Usar este componente en la app para evitar prop drilling del userId.
 */
export function ConnectedCheckInPanel() {
  const { user } = useAuth()
  return <CheckInPanel currentUserId={user?.userId ?? ''} />
}
