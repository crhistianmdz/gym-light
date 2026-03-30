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

### HU-06 — Auditoría de Check-ins
* **Retención de datos:** Logs conservados durante 3 años.
* **Filtros disponibles:** Fecha (FromDate, ToDate), Recepcionista (performedByUserId), Socio (memberId), Resultado (Allowed, Denied).
* **Exportación:** CSV implementado; exportación en PDF planificada para Fase 2.
* **Restricciones de acceso:** Solo Admin y Owner tienen acceso al panel de auditoría.
* **Validación:** Campo `performedByUserId` obligatorio en todo check-in, respondiendo con 400 Bad Request si está vacío o es Guid.Empty.