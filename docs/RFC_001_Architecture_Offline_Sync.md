# RFC 001: Arquitectura de Sincronización, Idempotencia y Resiliencia

**Estado:** Aprobado para Implementación  
**Versión:** 1.2  
**Tecnologías:** .NET 8, React, Dexie.js (IndexedDB), Service Workers, material design.

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