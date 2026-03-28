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

### HU-04: Sincronización Automática e Idempotente
**Como** Sistema, **quiero** subir los registros acumulados al detectar red, **para** asegurar la integridad de la base central.
* **Criterios de Aceptación:**
    1. Disparar proceso al detectar evento `online` o cada 5 min (Retry Policy).
    2. El backend debe ignorar duplicados basados en `ClientGuid` y responder `200 OK`.
    3. Limpiar la cola local solo tras la confirmación exitosa del servidor.

### HU-05: Autenticación Robusta y Sesión Offline
**Como** Usuario, **quiero** mantener mi sesión activa de forma segura, **para** operar la PWA incluso sin conexión.
* **Criterios de Aceptación:**
    1. Uso de `HttpOnly Cookies` para Refresh Tokens.
    2. Solicitar `navigator.storage.persist()` para proteger la base de datos local.

### HU-06: Auditoría de Transacciones (Logs)
**Como** Administrador, **quiero** ver un registro de las acciones realizadas por cada usuario, **para** auditar la operación y prevenir fraudes.
* **Criterios de Aceptación:**
    1. Cada acción (pago, acceso, venta) debe quedar asociada al ID del usuario que la ejecutó.

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
**Como** Entrenador, **quiero** registrar medidas físicas del socio, **para** demostrar el valor del servicio.
* **Criterios de Aceptación:**
    1. Formulario para: peso, % grasa, pecho, cintura, cadera, brazo y pierna.

### HU-10: Visualización de Evolución (Gráficas)
**Como** Socio, **quiero** ver gráficas de mi progreso físico, **para** mantenerme motivado.
* **Criterios de Aceptación:**
    1. Generar gráfica de líneas comparativa entre las distintas tomas de medidas.

### HU-11: Asignación de Rutinas Digitales
**Como** Entrenador, **quiero** armar rutinas para mis socios, **para** guiar su entrenamiento.
* **Criterios de Aceptación:**
    1. El socio debe poder marcar cada ejercicio como "completado" desde la PWA.

### HU-12: Dashboard de Métricas (Dueño)
**Como** Dueño, **quiero** ver el flujo de caja y la tasa de deserción (Churn Rate), **para** tomar decisiones estratégicas.
* **Criterios de Aceptación:**
    1. Reporte de ingresos mensuales detallado por categoría (Planes vs POS).
    2. Comparativa de socios activos vs. socios que no renovaron.