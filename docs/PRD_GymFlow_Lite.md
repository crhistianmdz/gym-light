# Product Requirement Document (PRD) - GymFlow Lite

## 1. Visión y Objetivo
Desarrollar una plataforma de gestión para gimnasios de alto tráfico que garantice la **continuidad operativa total** mediante una arquitectura Offline-First. El sistema debe permitir el control de acceso y ventas sin interrupciones, independientemente de la estabilidad de la conexión a internet.

## 2. Roles y Permisos (RBAC)
| Rol | Responsabilidad Clave |
| :--- | :--- |
| **Dueño** | Visualización de KPIs financieros, Churn Rate y gestión multi-sede. |
| **Administrador** | Configuración de planes, auditoría de logs y validación de pagos. |
| **Recepcionista** | Operación diaria: Check-in, registro de socios y ventas POS. |
| **Entrenador** | Creación de rutinas digitales y seguimiento antropométrico. |
| **Socio** | Consulta de progreso y check-list de rutinas. |

## 3. Alcance del Proyecto por Fases

### Fase 1: MVP & Operación Core
*   **Gestión de Usuarios:** Registro con captura de foto obligatoria (WebP) para validación de identidad.
*   **Control de Acceso Offline:** Validación contra caché local (IndexedDB) en <200ms.
*   **Suscripciones y Pagos:** Ciclo de vida de membresías (Activa/Vencida) con registro de confirmación externa.
*   **Punto de Venta (POS):** Venta de productos con **alerta de stock crítico al alcanzar el 20%**.
*   **Sincronización:** Motor de sincronización idempotente para subir logs locales al detectar red.
*   **Auditoría Básica:** Registro de "quién hizo qué" (Logs de transacciones) para evitar fraudes en recepción.

### Sincronización Automática (HU-04)

**Motor de sync**: SyncService en app JS (no Service Worker). Corre dentro de la pestaña activa.

**Disparadores**: Evento `window.online` + timer cada 5 minutos.

**Estrategia**: Reintento individual — cada evento de `sync_queue` se envía contra su endpoint original.

**Idempotencia**: El servidor responde `200 OK` con `{ alreadyProcessed: true, data: <entidad> }` cuando recibe un `ClientGuid` ya procesado. El cliente elimina el item de la cola y re-hidrata su cache local con `data`.

**Política de reintentos**: Máximo 3 intentos por item. Al superar el límite, el item se mueve individualmente a `error_queue`. La cola continúa procesando el resto.

**Locking multi-tab**: Un flag en `db.metadata` (`syncLock: boolean`) evita que múltiples pestañas procesen la cola simultáneamente.

**Token caducado**: Si el servidor responde `401`, SyncService pausa la cola y notifica la UI para re-login. Reanuda automáticamente tras autenticación exitosa.

**Reconciliación**: El servidor es autoritativo. Cada respuesta exitosa incluye la entidad actualizada. El cliente sobreescribe su cache local con esos valores.

**Error-tray**: Items en `error_queue` son visibles para Admin/Owner. Pueden ser reintentados manualmente o descartados.