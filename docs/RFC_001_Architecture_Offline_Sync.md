# RFC 001: Arquitectura de Sincronización, Idempotencia y Resiliencia

**Estado:** Aprobado para Implementación  
**Versión:** 1.3  
**Tecnologías:** .NET 8, React, Dexie.js (IndexedDB), Service Workers, Material Design (MUI), recharts.

## 1. Introducción
Este documento define la estrategia técnica para garantizar que GymFlow Lite opere con un modelo "Offline-First". El sistema prioriza la **Disponibilidad** sobre la **Consistencia Fuerte** inmediata, utilizando sincronización asíncrona e idempotente.

## 2. Persistencia Local (Client-Side)
Se utilizará **IndexedDB** a través de la librería `Dexie.js` para gestionar el almacenamiento estructurado en el navegador, superando el límite de 5MB de LocalStorage.

### 2.1. Esquema de Base de Datos Local
*   **Store `users`:** Cache de socios activos para validación visual y lógica.
    *   `id`, `fullName`, `photoWebP`, `status` (Active, Frozen, Expired), `membershipEndDate`.
*   **Store `sync_queue`:** Cola de eventos pendientes por procesar en el servidor.
    *   `guid` (UUID v4 generado en el cliente).
    *   `type` (CheckIn, Sale, SaleCancel, HealthUpdate).
    *   `payload` (JSON con datos de la transacción).
    *   `timestamp`.
*   **Store `error_queue`:**
    Necesaria para almacenar eventos que fallaron 3 veces durante la sincronización.
    ```typescript
    db.version(3).stores({
      // stores anteriores sin cambios...
      error_queue: 'guid, type, timestamp, retryCount'
    })

    interface ErrorQueueItem {
      guid: string
      type: SyncEventType
      payload: string
      timestamp: number
      retryCount: number
      lastError: string
      failedAt: number
    }
    ```
*   **Store `metadata`:** 
    *   `dataVersion`: Versión actual del esquema local.
    *   `lastSyncTimestamp`: Última vez que se descargó el snapshot del servidor.
    *   `syncLock`: Evita que múltiples pestañas procesen la cola simultáneamente (boolean).

### Estructura de Datos:
```typescript
db.version(2).stores({
  users:      'id, status, membershipEndDate',
  sync_queue: 'guid, type, timestamp',
  metadata:   'key',
  products:   'id, name, sku, stock',   // nuevo — catálogo local
  sales:      'id, clientGuid, status, timestamp',  // nuevo — historial local
})
```

## 3. Estrategia de Red y Service Worker
Se confirma que **NO** se implementará Service Worker en Fase 1. En su lugar, la lógica de sincronización estará a cargo del `SyncService`, dentro de la pestaña activa.

### Proceso de Sincronización
```javascript
processQueue():
  1. Verificar syncLock en metadata → si locked, abortar
  2. Setear syncLock = true
  3. Leer sync_queue ordenado por timestamp
  4. Por cada item:
     a. fetch(endpoint, payload, headers: { X-Client-Guid: item.guid })
     b. Si 200/201: eliminar de sync_queue, actualizar cache local con response.data
     c. Si 200 con alreadyProcessed=true: eliminar de sync_queue, re-hidratar cache
     d. Si 401: pausar cola, emitir evento 'sync:auth-required', break
     e. Si 4xx (no 401): incrementar retryCount; si >= 3 → mover a error_queue
     f. Si 5xx o timeout: incrementar retryCount; si >= 3 → mover a error_queue
  5. Liberar syncLock = false
```

## 4. Lógica de Sincronización y Conflictos
Para asegurar la integridad entre el cliente y el servidor:

*   **Idempotencia:** El backend (.NET 8) implementará un `IdempotencyFilter`. Si un `ClientGuid` ya existe, el servidor responderá `200 OK` (duplicado detectado) pero no procesará la lógica de nuevo, permitiendo al cliente limpiar su cola de forma segura.
*   **Autoridad del Servidor (Server Authority):** En caso de edición simultánea, los datos del servidor prevalecen. El cliente sobrescribe su caché local con la respuesta del servidor.
*   **Stock autoritativo:** En caso de divergencia entre stock local y stock del servidor, el servidor siempre gana. Al confirmar una venta o al sincronizar, el frontend sobrescribe el stock local con el valor del servidor.
*   **Migración de Datos:** Cada respuesta de la API incluirá un header `X-Data-Version`. Si `X-Data-Version > metadata.dataVersion`, el cliente disparará una limpieza de IndexedDB y descargará los datos maestros de nuevo para evitar errores de esquema.

## 5. Reglas de Negocio Técnicas
*   **MembershipService (.NET):** 
    *   Validación de Congelamiento: `count < 4` por año y `duration >= 7` días.
    *   Cálculo de `EndDate`: Suma de días de congelación efectiva al vencimiento original.
*   **InventoryService:**
    *   Bloqueo de venta si el stock local es `0`.  
    *   Notificación de stock crítico al llegar al `20%`.
    *   Confirmación con autoridad del servidor para ajustes de stock.

## 6. Visualización de Datos (Frontend)

### 6.1. Librería de Gráficas
Se utiliza **`recharts`** para todas las visualizaciones del frontend:
- Criterios de selección: React-first (API JSX declarativa), bundle ~150kb gzipped, mantenimiento activo, sin dependencias de Canvas/SVG externo.
- Alternativas descartadas: `chart.js` (más pesada, API imperativa), `victory` (menor adopción).

### 6.2. Gráfica de Evolución Física (HU-10)
Componente `ProgressChart` — `LineChart` de recharts montado dentro de una `MUI Card`.

**Contrato de datos:**
```typescript
// Entrada: array de mediciones de HU-09
interface MeasurementLocal {
  id: string
  memberId: string
  recordedAt: string       // ISO 8601
  unitSystem: 0 | 1        // 0=Metric, 1=Imperial
  weightKg: number
  bodyFatPct: number
  chestCm: number
  waistCm: number
  hipCm: number
  armCm: number
  thighCm: number
}

// Variable seleccionable
type MeasurementKey = 'weightKg' | 'bodyFatPct' | 'chestCm' | 'waistCm' | 'hipCm' | 'armCm' | 'thighCm'
```

**Reglas de visualización:**
- Eje X: `RecordedAt` formateado como `DD/MM/YYYY`.
- Eje Y: valor numérico del campo seleccionado, sin conversión.
- Tooltip: fecha + valor + unidad derivada del `UnitSystem` de esa toma específica.
- 0 tomas → mensaje vacío (sin gráfica). 1 toma → punto solo (`dot`, sin `line`). 2+ tomas → `LineChart` completa.

**Unidades por campo y sistema:**
| Campo        | Metric | Imperial |
|--------------|--------|----------|
| weightKg     | kg     | lbs      |
| bodyFatPct   | %      | %        |
| chestCm      | cm     | in       |
| waistCm      | cm     | in       |
| hipCm        | cm     | in       |
| armCm        | cm     | in       |
| thighCm      | cm     | in       |

**RBAC:**
- `Member`: solo puede ver su propio `memberId` (guard en componente y en hook).
- `Trainer`, `Admin`, `Owner`: acceso a cualquier `memberId`.

**Offline:**
- Fuente de datos: `measurementService.getByMember(memberId)` — resuelve desde IndexedDB si sin conexión (misma lógica de HU-09).

## 7. Rutinas Digitales (HU-11)

### 7.1. Modelo de Dominio

| Entidad | Propósito |
|---|---|
| `ExerciseCatalog` | Biblioteca global de ejercicios (nombre, descripción, mediaUrl opcional) |
| `Routine` | Plantilla de rutina creada por Trainer/Admin/Owner |
| `RoutineExercise` | Ejercicio dentro de una Routine (orden, sets, reps, notes, ExerciseCatalogId nullable) |
| `RoutineAssignment` | Asignación de una Routine a un Member por un Trainer/Admin/Owner |
| `WorkoutLog` | Registro de sesión de entrenamiento del Member |
| `WorkoutExerciseEntry` | Estado de cada ejercicio en el WorkoutLog (Completed boolean) |

### 7.2. Esquema Dexie.js (versión 5)

```typescript
db.version(5).stores({
  // stores anteriores sin cambios...
  exercise_catalog: 'id, name, isCustom',
  routines:         'id, createdByUserId, isPublic, updatedAt',
  routine_assignments: 'id, routineId, memberId, assignedAt',
  workout_logs:     'id, assignmentId, memberId, clientGuid, syncStatus',
})
```

### 7.3. Eventos de Sincronización (sync_queue)

Nuevos tipos de evento:
- `WorkoutLogCreate`: payload `{ assignmentId, memberId, entries: [{routineExerciseId, completed, completedAt}], clientGuid }`
- `ExerciseComplete`: payload `{ workoutLogId, routineExerciseId, completed, clientGuid }`

### 7.4. RBAC por endpoint

| Endpoint | Roles permitidos |
|---|---|
| `POST /api/routines` | Trainer, Admin, Owner |
| `PUT /api/routines/{id}` | Trainer (propias), Admin, Owner |
| `GET /api/routines` | Trainer, Admin, Owner |
| `POST /api/routine-assignments` | Trainer, Admin, Owner |
| `GET /api/members/{id}/routines` | Member (solo propio), Trainer, Admin, Owner |
| `POST /api/workout-logs` | Member (solo propio), Trainer, Admin, Owner |

### 7.5. Offline Strategy

- Estrategia idéntica a HU-09: Network-First con fallback a IndexedDB.
- El Member puede marcar ejercicios offline → encola `WorkoutLogCreate` en `sync_queue` con `clientGuid`.
- Al reconectar, `syncService` procesa la cola; el backend responde `200 OK` si `clientGuid` ya existe (idempotencia).
- Conflict resolution: server wins (consistente con RFC sección 4).

### 7.6. Granularidad de tracking

- Nivel: **por ejercicio** — boolean `Completed` + `CompletedAt` (timestamp).
- Sin tracking de sets/reps/peso en esta versión.
- `WorkoutLog` registra la sesión completa (array de entries).