# Backlog de Historias de Usuario - GymFlow Lite (Versión Final)

## Épica 1: Operación Base y Disponibilidad (Fase 1)

### HU-01: Validación de Acceso Offline
**Como** Recepcionista, **quiero** validar el ingreso de un socio aunque no haya internet, **para** evitar filas y no interrumpir la operación.
* **Criterios de Aceptación:**
    1. Si el API falla o hay timeout (>2s), consultar la store `users` en IndexedDB.
    2. Mostrar foto de perfil cacheada (WebP) para verificación visual.
    3. Registrar el `AccessLog` en `sync_queue` con un `ClientGuid`.

### HU-02: Registro de Socio con Foto Obligatoria
**Como** Recepcionista, **quiero** crear el perfil de un socio incluyendo su foto, **para** prevenir la suplantación de identidad.
* **Criterios de Aceptación:**
    1. El campo "Foto" es obligatorio para habilitar el botón de guardado.
    2. El frontend debe comprimir la imagen a WebP antes de enviarla o cachearla.

### HU-03 — Venta de Producto / Consumo en Mostrador
**Como** recepcionista/admin/owner,
**quiero** registrar ventas de productos con múltiples ítems,
**para** controlar el stock y registrar ingresos del gimnasio.

### Criterios de Aceptación

**CA-01 — Alerta de stock bajo**  
Dado que un producto tiene `stock <= initialStock * 0.20`,  
cuando el recepcionista visualiza el catálogo o selecciona el producto,  
entonces se muestra una alerta visual crítica (banner rojo) indicando stock bajo.

**CA-02 — Bloqueo por stock cero**  
Dado que un producto tiene `stock == 0`,  
cuando el recepcionista intenta agregarlo a la venta,  
entonces el botón de venta está deshabilitado y el servidor rechaza la operación.

**CA-03 — Venta multi-item**  
Dado que el recepcionista selecciona múltiples productos con cantidades,  
cuando confirma la venta,  
entonces se registra una `Sale` con sus `SaleLine` correspondientes y se descuenta el stock de cada producto.

**CA-04 — Idempotencia**  
Dado que una venta fue enviada con un `ClientGuid`,  
cuando el servidor recibe el mismo `ClientGuid` por segunda vez,  
entonces responde `200 OK` sin reprocesar ni duplicar el descuento de stock.

**CA-05 — Cancelación de venta**  
Dado que existe una venta registrada,  
cuando un Admin u Owner la cancela,  
entonces el stock de cada producto en las `SaleLine` se restaura dentro de una transacción atómica.

**CA-06 — Operación offline**  
Dado que no hay conexión a internet,  
cuando el recepcionista registra una venta,  
entonces la venta se encola en `sync_queue` con type `'Sale'` y se sincroniza automáticamente al recuperar la conexión.

**CA-07 — Reconciliación de stock**  
Dado que el stock local difiere del stock del servidor tras sincronizar,  
entonces el cliente sobreescribe su valor local con el valor del servidor (servidor autoritativo).

**CA-08 — ABM de productos**  
Dado que el admin gestiona el catálogo,  
cuando crea, edita o elimina un producto,  
entonces los cambios se reflejan en la base de datos y en el catálogo local offline.

**CA-09 — Seed de productos**  
El entorno de desarrollo incluye un seed con al menos 10 productos de prueba con stock inicial definido.

**CA-10 — Roles**  
Solo los roles `Receptionist`, `Admin` y `Owner` pueden registrar ventas.  
Solo `Admin` y `Owner` pueden cancelar ventas o gestionar el catálogo (ABM).

### HU-04 — Sincronización Automática e Idempotente
**Como** sistema,
**quiero** sincronizar automáticamente los registros pendientes cuando hay conexión,
**para** garantizar la integridad de los datos entre el cliente offline y el servidor.

#### Criterios de Aceptación

**CA-01 — Disparo automático**  
Al recuperar conexión (evento `online`) o cada 5 minutos, el SyncService procesa la `sync_queue` automáticamente.

**CA-02 — Idempotencia servidor**  
Dado que el servidor recibe un `ClientGuid` ya procesado,  
entonces responde `200 OK` con `{ alreadyProcessed: true, data: <entidad> }`.  
El cliente elimina el item de la cola y re-hidrata su cache con `data`.

**CA-03 — Limpieza de cola**  
Un item se elimina de `sync_queue` únicamente tras confirmación exitosa del servidor (200 o 201).

**CA-04 — Política de reintentos**  
Si un item falla, se incrementa su `retryCount`.  
Al llegar a 3 fallos, se mueve a `error_queue` y la cola continúa con el siguiente item.

**CA-05 — Error-tray**  
Los items en `error_queue` son visibles para Admin/Owner. Pueden ser reintentados manualmente o descartados desde la UI.

**CA-06 — Locking multi-tab**  
Si múltiples pestañas están abiertas, solo una procesa la cola simultáneamente (flag `syncLock` en `db.metadata`).

**CA-07 — Token caducado**  
Si el servidor responde `401` durante la sincronización, SyncService pausa la cola y notifica la UI para re-login. Reanuda tras autenticación exitosa.

**CA-08 — Reconciliación autoritativa**  
El servidor es autoritativo. Cada respuesta exitosa incluye la entidad actualizada. El cliente sobreescribe su cache local (stock, estado de membresía, etc.) con esos valores.

**CA-09 — Indicador visual**  
El componente `SyncStatusBadge` refleja en tiempo real:  
- 🟢 Verde: cola vacía, todo sincronizado  
- 🟠 Naranja: items pendientes en `sync_queue`  
- ⚫ Gris: sin conexión (offline)  
- 🔴 Rojo: items en `error_queue`

**CA-10 — Tipos soportados**  
La `sync_queue` procesa eventos de tipo: `CheckIn`, `Sale`, `SaleCancel`, `MemberUpdate`.