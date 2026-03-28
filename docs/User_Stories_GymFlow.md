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

### HU-03: Venta POS con Alerta de Stock y Bloqueo en Cero
**Como** Recepcionista, **quiero** vender productos y ver alertas de inventario, **para** mantener la rotación de existencias.
* **Criterios de Aceptación:**
    1. Mostrar alerta visual crítica cuando el stock local sea `<= 20%`.
    2. **Bloqueo:** No permitir la venta (botón deshabilitado) si el stock local es `0`.
    3. Cada venta genera un registro en la cola de sincronización.

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