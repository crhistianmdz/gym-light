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

### HU-05: Autenticación Robusta y Sesión Offline
**Como** Usuario, **quiero** mantener mi sesión activa de forma segura, **para** operar la PWA incluso sin conexión.
* **Criterios de Aceptación:**
    1. Uso de `HttpOnly Cookies` para Refresh Tokens.
    2. Solicitar `navigator.storage.persist()` para proteger la base de datos local.

### HU-06: Auditoría de Check-ins
**Como** Administrador, **quiero** auditar los logs de acceso, **para** detectar anomalías y garantizar la trazabilidad total de check-ins.
#### Criterios de Aceptación
1. **Filtros:** Admin/Owner puede filtrar registros por rango de fechas, recepcionista (performedByUserId), socio (memberId) y resultado (Allowed/Denied).
2. **Exportación:** Permite exportar los datos filtrados en formato CSV correctamente.
3. **Validación:** Check-in que reciba un `performedByUserId` vacío devuelve un error `400 Bad Request` y no persiste el registro.
4. **Idempotencia:** Logs duplicados basados en `ClientGuid` son ignorados gracias al índice único.
5. **Restricciones:** Solo Admin y Owner pueden acceder al panel, mientras que Receptionist recibe un 403 Forbidden.
6. **Retención de datos:** Solo aparecen registros con menos de 3 años (archivados automáticamente al superar este límite).

## Épica 2: Gestión Avanzada y Fidelización (Fase 2)

### HU-07: Lógica de Congelamiento de Membresía
**Como** Administrador, **quiero** pausar la suscripción de un socio, **para** gestionar solicitudes de viaje o salud.
* **Criterios de Aceptación:**
    1. Validar máximo **4 congelamientos por año**.
    2. Validar duración mínima de **7 días**.
    3. Denegar acceso inmediato y recalcular `EndDate` sumando los días de pausa.

### HU-08: Cancelación con Acceso Residual (No Reembolso)
**Como** Socio, **quiero** cancelar mi suscripción pero seguir asistiendo el tiempo restante, **para** aprovechar el periodo pagado.
* **Criterios de Aceptación:**
    1. El sistema marca "No renovación" sin generar reembolsos automáticos.
    2. El acceso sigue siendo permitido hasta la fecha de expiración original.

### HU-09: Perfil Antropométrico y Progreso
**Como** Entrenador o Socio, **quiero** registrar medidas físicas del socio, **para** demostrar el valor del servicio y hacer seguimiento del progreso personal.
* **Criterios de Aceptación:**
    1. Formulario con 7 campos **todos obligatorios**: peso, % grasa, pecho, cintura, cadera, brazo y pierna.
    2. Tanto el **Entrenador** como el **Socio** pueden registrar medidas (el Socio solo puede registrar las suyas propias).
    3. El sistema soporta **dos sistemas de unidades**: métrico (kg / cm) e imperial (lbs / inches). El usuario selecciona el sistema al registrar; la UI muestra las etiquetas correspondientes.
    4. Las medidas se almacenan junto al `unitSystem` utilizado al momento del registro.
    5. El registro de medidas soporta modo **offline**: se encola en `sync_queue` con tipo `HealthUpdate` y se sincroniza automáticamente al recuperar la conexión.
    6. Las medidas se listan ordenadas por fecha de toma (más reciente primero).
    7. Solo `Trainer`, `Admin`, `Owner` y el propio `Member` pueden acceder al historial de medidas de un socio.

* **Decisiones de PO (2026-03-30):**
    - RBAC POST: `Trainer`, `Admin`, `Owner`, `Member` (ownership propio)
    - RBAC GET: `Trainer`, `Admin`, `Owner`, `Member` (ownership propio)
    - Campos: todos obligatorios (sin nullables)
    - Unidades: métrico (`kg`/`cm`) e imperial (`lbs`/`inches`) — campo `unitSystem` en entidad

### HU-10: Visualización de Evolución (Gráficas)
**Como** Socio (y roles autorizados), **quiero** ver gráficas de mi progreso físico, **para** mantenerme motivado y hacer seguimiento de mi evolución.
* **Criterios de Aceptación:**
    1. Generar gráfica de líneas comparativa entre las distintas tomas de medidas (datos de HU-09).
    2. El usuario puede seleccionar cuál de las 7 medidas desea graficar mediante un selector (WeightKg, BodyFatPct, ChestCm, WaistCm, HipCm, ArmCm, ThighCm). Variable por defecto: Peso.
    3. Los valores se muestran tal como fueron registrados, respetando el `UnitSystem` de cada toma (sin conversión). El tooltip indica la unidad individual de cada punto.
    4. Si hay 0 tomas: mostrar mensaje "Aún no hay medidas registradas." Si hay 1 toma: mostrar el punto en la gráfica (sin línea). Si hay 2+: gráfica de líneas completa.
    5. RBAC: el Socio solo puede ver sus propias gráficas. Trainer, Admin y Owner pueden ver las gráficas de cualquier socio.
    6. La gráfica se visualiza desde el tab "Progreso" dentro de la pantalla de detalle del socio (`MemberDetail`).
    7. Soporte offline-first: si no hay conexión, se usan los datos del store `measurements` de IndexedDB (Dexie.js).
* **Decisiones Técnicas (PO aprobadas 2026-03-31):**
    - Librería de gráficas: `recharts` (React-first, bundle ~150kb, mantenimiento activo).
    - UI Kit: Material Design (MUI) — Card para contenedor, Select para selector de variable, CircularProgress para loading.
    - Sin conversión de unidades en cliente: el valor se grafica as-is con su unidad original en el tooltip.

### HU-11: Asignación de Rutinas Digitales
**Como** Entrenador, **quiero** armar rutinas para mis socios, **para** guiar su entrenamiento.
* **Criterios de Aceptación:**
    1. El socio debe poder marcar cada ejercicio como "completado" desde la PWA.

### HU-12: Dashboard de Métricas (Dueño)
**Como** Dueño, **quiero** ver el flujo de caja y la tasa de deserción (Churn Rate), **para** tomar decisiones estratégicas.
* **Criterios de Aceptación:**
    1. Reporte de ingresos mensuales detallado por categoría (Planes vs POS).
    2. Comparativa de socios activos vs. socios que no renovaron.