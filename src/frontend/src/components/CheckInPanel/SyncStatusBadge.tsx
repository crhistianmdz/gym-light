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
  const errorCount = useLiveQuery(() => db.error_queue.count(), [], 0);
  const pendingCount = useLiveQuery(
    () => db.sync_queue.where('type').equals('Sale').or('type').equals('SaleCancel').count(),
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
    <>
      {errorCount > 0 && (
        <span style={{ color: '#f44336', fontWeight: 600, fontSize: 13, marginLeft: 8 }} title={`${errorCount} errores por resolver`}>
          2c10 {errorCount}
        </span>
      )}
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
   )}
}
