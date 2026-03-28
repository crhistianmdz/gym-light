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
*   **Store `metadata`:** 
    *   `dataVersion`: Versión actual del esquema local.
    *   `lastSyncTimestamp`: Última vez que se descargó el snapshot del servidor.
*   **Store `products`:**
    *   `id`, `name`, `sku`, `stock`.  
*   **Store `sales`:**
    *   `id`, `clientGuid`, `status`, `timestamp`.

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

#### Tipos TypeScript:
```typescript
interface ProductLocal {
  id: string
  sku?: string
  name: string
  description?: string
  price: number
  stock: number           // stock actual
  initialStock: number    // para calcular umbral 20%
  reorderThreshold?: number
  updatedAt: number
}

interface SaleLocal {
  id: string
  clientGuid: string
  lines: SaleLineLocal[]
  total: number
  status: 'pending' | 'synced' | 'cancelled'
  timestamp: number
  isOffline: boolean
  retryCount: number
}

interface SaleLineLocal {
  productId: string
  productName: string
  quantity: number
  unitPrice: number
  subtotal: number
}
```

## 3. Estrategia de Red y Service Worker
El Service Worker actuará como un proxy inteligente siguiendo la estrategia **Network-First with Fallback**.

1.  **Interceptación:** El SW captura peticiones a la API.
2.  **Modo Online:** Ejecuta el `fetch`. Si tiene éxito, actualiza la store `users` local (re-hidratación).
3.  **Modo Offline:** Si falla la red o hay timeout (>2s), el SW valida contra IndexedDB y encola la acción en `sync_queue` con el flag `isOffline: true`.

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

## 6. Observabilidad y UX
*   **Status Indicador:** 
    *   `Verde`: Sincronizado.
    *   `Naranja`: Online con `X` registros pendientes.
    *   `Gris`: Modo Offline (Operando con caché local).
*   **Retry Policy:** El cliente reintentará la sincronización cada 5 minutos o tras el evento `online`. Si un registro falla 3 veces, pasará a una "Bandeja de Errores" para revisión manual.

## 7. Seguridad
*   **Auth:** JWT con Refresh Tokens en `HttpOnly Cookies`.
*   **Persistencia de Sesión:** El sistema solicitará `navigator.storage.persist()` para evitar que el navegador elimine la base de datos IndexedDB durante limpiezas automáticas de caché.