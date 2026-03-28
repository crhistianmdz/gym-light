# HU-04 — Sincronización Automática e Idempotente

## Resumen
HU-04 implementa un sistema de sincronización automática para transacciones offline garantizando idempotencia. Esto permite que los usuarios operen en entornos sin conexión, mientras que las transacciones se sincronizan automáticamente cuando el dispositivo recupera la conectividad.

### Objetivos:
- Sincronización robusta de datos locales con el servidor.
- Tolerancia a fallas temporales de red.
- Evitar duplicados mediante el uso de ClientGuid y un filtro de idempotencia.

---

## Arquitectura de sincronización

### Decisiones de diseño

- **SyncService en app JS (no Service Worker):** La decisión de implementarlo dentro de la aplicación (en lugar de un Service Worker) proporciona mayor flexibilidad y simplicidad al tiempo que mantiene el control bajo el contexto de React.
- **Endpoints individuales:** Las transacciones se envían como registros individuales en lugar de procesar lotes. Esto mejora la trazabilidad y permite manejar errores de forma granular por ítem.
- **Error-tray por ítem individual:** Al mover los errores a una cola de "ErrorTray", cada registro problemático se puede gestionar sin bloquear el resto de las transacciones pendientes.

### Flujo completo
1. Usuario genera una operación offline → **sync_queue.**
2. **SyncService** detecta cambios y envía el payload al endpoint correspondiente.
3. **API Backend:** Valida el `X-Client-Guid` para garantizar idempotencia.
4. Resultado del endpoint determina:
   - Éxito: Actualiza caché local.
   - Falla: Incrementa reintentos o mueve a `error_queue`.

---

## SyncService

### Ciclo de vida

- **Inicio:**
  - Iniciado automáticamente al montar `<AuthProvider>`.
  - Listener para eventos de red y activación de sincronización en intervalos (`SYNC_INTERVAL_MS`).
- **Parada:**
  - Detiene listeners y temporizadores al desmontar o eventos críticos como logout.

### `processQueue()` — Paso a paso

1. Confirma conectividad (`navigator.onLine`).
2. Adquiere candado `syncLock` en `metadata` para evitar concurrencia multi-tab.
3. Itera sobre `sync_queue`, enviando cada ítem al endpoint correspondiente.
4. Respuesta API:
   - **200 OK** → Actualiza caché local y elimina de la cola.
   - **401** → Dispara `sync:auth-required`, pausa la sincronización.
   - **Otros errores** → Incrementa reintentos o mueve a `error_queue`.
5. Libera el `syncLock` y emite evento `sync:completed`.

### Política de reintentos
- **MAX_RETRIES = 3.**
- Cada falla aumenta el contador `retryCount`.
- Si supera el límite → Mueve el registro a `error_queue` con detalles.

### Eventos `CustomEvent` emitidos

| Evento              | Cuándo                          | Detail                                    |
|---------------------|---------------------------------|-------------------------------------------|
| `sync:started`      | Al iniciar un ciclo de sync     | N/A                                       |
| `sync:completed`    | Al finalizar un ciclo de sync   | N/A                                       |
| `sync:auth-required`| Token expirado (HTTP 401)       | N/A                                       |
| `sync:item-failed`  | Error en un ítem                | `{ guid, type, error }`                   |

---

## Idempotencia

### Backend — `IdempotencyFilter`
- **Función:** Evalúa header `X-Client-Guid` para detectar duplicados en AccessLogs.
- **Respuesta en caso de duplicado:**
  ```json
  {
    "alreadyProcessed": true,
    "data": { "...entidad..." }
  }
  ```

### Frontend — `ClientGuid`
- **Generación:** UUID v4.
  - Incluido como header en cada request.
  - Generado al guardar localmente en `sync_queue`.

---

## Dexie.js — Stores HU-04

### `sync_queue`
- **Schema:**
  ```
  guid, type, payload, timestamp, retryCount.
  ```
- **Uso:**
  - Se agrega al iniciar transacciones offline.
  - Eliminado tras éxito en sincronización.

### `error_queue`
- **Schema:**
  ```
  guid, type, payload, retryCount, lastError, failedAt.
  ```
- **Uso:**
  - Ítem fallido pasa después de `MAX_RETRIES`.
  - Limpiado manualmente desde `ErrorTrayPanel`.

---

## Componentes UI

### `SyncStatusBadge`

- **Estados:**
  - Verde → `0` pendientes.
  - Naranja → Registros en `sync_queue` (ej.: `10 pendientes`).
  - Gris → Modo offline.

### `SyncManagerPanel`
- **Rol:** Administra registros pendientes y fallidos.
- **Acciones:**
  - Reintento manual de errores.
  - Limpieza de `error_queue` por ítem.

### `ErrorTrayPanel`
- **Rol:** Vista dedicada a errores de sync.
- **Acciones:**
  - Reintento manual.
  - Descartar errores seleccionados.

---

## Locking multi-tab
- **SyncLock:** Registro booleano en `metadata`.
- Previene colisiones y duplicación de procesos entre pestañas activas simultáneamente.

---

## Manejo de token caducado (401)
1. Detiene el ciclo y emite `sync:auth-required`.
2. Actualiza el token (con `refreshToken`).
3. Reanuda automáticamente o exige login manual (según caso).

---

## Archivos clave

| Archivo                                                | Responsabilidad                               |
|--------------------------------------------------------|-----------------------------------------------|
| `src/frontend/src/services/syncService.ts`             | Motor de sincronización.                     |
| `src/frontend/src/db/gymflow.db.ts`                    | Persistencia en IndexedDB.                   |
| `src/backend/WebAPI/Filters/IdempotencyFilter.cs`      | Filtro idempotente en Server.                |
| `src/frontend/src/components/Sync/SyncManagerPanel.tsx`| UI de control para sincronización. |
| `src/frontend/src/components/Admin/ErrorTrayPanel.tsx` | Panel de gestión de errores de sync. |
| `src/frontend/src/components/CheckInPanel/SyncStatusBadge.tsx` | Badge de estado de sincronización. |
