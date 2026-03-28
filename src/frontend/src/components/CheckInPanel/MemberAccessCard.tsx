import type { MemberStatus } from '@/db/gymflow.db'

interface MemberAccessCardProps {
  fullName: string
  photoWebP: string
  status: MemberStatus
  membershipEndDate: string
  allowed: boolean
  denialReason: string | null
  source: 'online' | 'offline'
}

/**
 * Tarjeta de resultado de check-in.
 * Muestra foto WebP cacheada para verificación visual de identidad (HU-01 CA-2).
 * Color de borde: verde = acceso permitido | rojo = denegado.
 */
export function MemberAccessCard({
  fullName,
  photoWebP,
  status,
  membershipEndDate,
  allowed,
  denialReason,
  source,
}: MemberAccessCardProps) {
  const borderColor = allowed ? '#4caf50' : '#f44336'
  const statusLabel: Record<MemberStatus, string> = {
    Active: 'Activa',
    Frozen: 'Congelada',
    Expired: 'Vencida',
  }

  return (
    <div
      style={{ border: `3px solid ${borderColor}`, borderRadius: 8, padding: 16, maxWidth: 340 }}
      role="article"
      aria-label={`Resultado de acceso para ${fullName}`}
    >
      {/* Foto obligatoria — verificación visual de identidad */}
      <img
        src={photoWebP}
        alt={`Foto de ${fullName}`}
        style={{ width: 80, height: 80, objectFit: 'cover', borderRadius: '50%' }}
      />

      <h3 style={{ margin: '8px 0 4px' }}>{fullName}</h3>

      <p style={{ margin: '2px 0', fontSize: 13, color: '#666' }}>
        Membresía: <strong>{statusLabel[status]}</strong> · Vence: {membershipEndDate}
      </p>

      <p
        style={{
          marginTop: 12,
          fontWeight: 700,
          fontSize: 18,
          color: borderColor,
        }}
      >
        {allowed ? '✅ ACCESO PERMITIDO' : '🚫 ACCESO DENEGADO'}
      </p>

      {denialReason && (
        <p style={{ margin: '4px 0 0', fontSize: 13, color: '#f44336' }}>{denialReason}</p>
      )}

      {/* Indicador de fuente — útil para debugging en recepción */}
      {source === 'offline' && (
        <p style={{ marginTop: 8, fontSize: 11, color: '#9e9e9e' }}>
          ⚠️ Validado en modo offline (sin conexión)
        </p>
      )}
    </div>
  )
}
