import { useLiveQuery } from 'dexie-react-hooks'
import { db } from '@/db/gymflow.db'

/**
 * Badge de estado de sincronización.
 * RFC §6 — Observabilidad:
 *   Verde  → 0 pendientes en sync_queue
 *   Naranja → X registros pendientes
 *   Gris   → sin conexión (navigator.onLine === false)
 */
export function SyncStatusBadge() {
  const pendingCount = useLiveQuery(
    () => db.sync_queue.count(),
    [],
    0,
  )

  const isOnline = navigator.onLine

  const config = !isOnline
    ? { color: '#9e9e9e', label: '● Offline', title: 'Sin conexión — operando con caché local' }
    : pendingCount > 0
      ? { color: '#ff9800', label: `● ${pendingCount} pendientes`, title: `${pendingCount} registros esperando sincronización` }
      : { color: '#4caf50', label: '● Sincronizado', title: 'Todos los registros están sincronizados' }

  return (
    <span
      title={config.title}
      style={{
        color: config.color,
        fontWeight: 600,
        fontSize: 13,
        display: 'inline-flex',
        alignItems: 'center',
        gap: 4,
      }}
      aria-live="polite"
      aria-label={config.title}
    >
      {config.label}
    </span>
  )
}
