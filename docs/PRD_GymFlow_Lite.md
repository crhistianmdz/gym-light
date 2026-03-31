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
*   **Suscripciones y Pagos:** Ciclo de vida de membresías (Activa/Vencida) con registro de confirmación externa. Los cobros de membresías y servicios se registran como `Payment` (separado de `Sale`, que corresponde a ventas de productos físicos del POS). Cada `Payment` tiene una categoría (`Membership` o `POS`) que permite generar reportes de ingresos desglosados.
*   **Punto de Venta (POS):** Venta de productos con **alerta de stock crítico al alcanzar el 20%**.
*   **Sincronización:** Motor de sincronización idempotente para subir logs locales al detectar red.
*   **Auditoría Básica:** Registro de "quién hizo qué" (Logs de transacciones) para evitar fraudes en recepción.
*   **HU-12 — Dashboard de Métricas:** Dashboard para Owner/Admin con reporte de ingresos mensuales por categoría (Membresías vs POS) y tasa de churn (socios que no renovaron). Solo accesible por roles Owner y Admin.

### Fase 2: Fidelización y Gestión Avanzada
*   **Módulo de Congelamiento (Reglas Estrictas):** 
    *   Máximo 4 eventos por año calendario.
    *   Mínimo 7 días por evento.
    *   Bloqueo de acceso inmediato durante el periodo de congelación.
    *   Extensión automática de la fecha de vencimiento (`EndDate`) sumando los días pausados.
*   **Política de Cancelación:** Al cancelar, el socio mantiene el derecho de acceso hasta que su suscripción expire (Acceso Residual). No hay reembolsos parciales automáticos.
*   **Seguimiento de Salud:** Registro de medidas antropométricas (peso, % grasa, pecho, cintura, cadera, brazo, pierna) con soporte de unidades métricas (kg/cm) e imperiales (lbs/inches). Tanto el Entrenador como el Socio pueden registrar medidas (el Socio solo las propias). Soporte offline-first con sincronización automática. Gráficas de evolución física (HU-10).
*   **Rutinas Digitales:** Constructor de rutinas y seguimiento de cumplimiento para el socio.

## 4. Reglas de Negocio Consolidadas
1.  **Validación de Acceso:** No se permite el ingreso si la suscripción está vencida o congelada.
2.  **Seguridad de Identidad:** La foto de perfil es un requisito técnico para habilitar el check-in.
3.  **Prioridad de Datos:** En caso de conflicto entre local y nube, prevalece la **Autoridad del Servidor**.
4.  **Idempotencia:** Cada transacción local genera un UUID (ClientGuid) único para evitar duplicados en la sincronización.
5.  **Inventario:** El sistema debe impedir ventas offline si el stock local registrado es 0.

## 5. Especificaciones Técnicas (Stack SSD)
*   **Backend:** .NET 8 (Web API) con Clean Architecture.
*   **Frontend:** React (PWA) con Service Workers.
*   **Persistencia Local:** IndexedDB (vía Dexie.js).
*   **Seguridad:** JWT (JSON Web Tokens) con Refresh Tokens en HttpOnly Cookies.

## 6. Requerimientos No Funcionales
*   **Disponibilidad:** Capacidad de operar offline para check-in y ventas por tiempo indefinido.
*   **Rendimiento:** Latencia de respuesta en la interfaz de recepción inferior a 200ms.
*   **Observabilidad:** Indicador visual de estado de sincronización (Sincronizado/Pendiente/Offline).