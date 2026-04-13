# AGENTS.md — GymFlow Lite
Guía operativa para agentes de IA que trabajan en este repositorio.
La fuente de verdad del producto es `/docs`. **No escribas código que contradiga esos documentos.**
---
## 1. Estado del Proyecto
El proyecto tiene código fuente activo. Las HU 01–11 están implementadas (backend + frontend).
Toda implementación debe seguir el pipeline SSD definido en `.agents/AGENT.MD`.
```
/docs/PRD_GymFlow_Lite.md                 ← Reglas de negocio y alcance
/docs/RFC_001_Architecture_Offline_Sync.md ← Decisiones técnicas aprobadas
/docs/tasks/User_Stories_GymFlow.md             ← Criterios de aceptación por HU
/.agents/AGENT.MD                         ← Reglas del orquestador
```
---
## 2. Stack Obligatorio
| Capa | Tecnología |
|---|---|
| Backend | .NET 8 Web API — Clean Architecture |
| Frontend | React + Vite — PWA (Service Workers + Web Manifest) |
| DB Cloud | PostgreSQL o SQL Server vía Entity Framework Core |
| DB Local | IndexedDB vía **Dexie.js** |
| Auth | JWT + Refresh Tokens en **HttpOnly Cookies** |
| Caché | Redis (sesión de servidor) |
| UI Kit | Material Design |
| Infra | Docker (app, db, redis) |
---
## 4. Reglas de Negocio — Prohibido Ignorarlas
Estas reglas vienen del PRD y la RFC. Cualquier código que las viole es incorrecto por definición:
1. **Acceso:** Denegar si `status === 'Frozen' || status === 'Expired'`. Sin excepciones.
2. **Foto obligatoria:** El botón de guardado de socio debe estar deshabilitado si no hay foto. La imagen se convierte a **WebP** en el cliente antes de enviar o cachear.
3. **Autoridad del servidor:** En conflicto de datos local vs. nube → el servidor gana. El cliente sobreescribe su caché.
4. **ClientGuid:** Toda escritura local genera un `UUID v4`. El backend responde `200 OK` si ya procesó ese GUID (no reprocesa).
5. **Stock:** Bloquear venta si stock local es `0`. Mostrar alerta crítica si stock `<= 20%`.
6. **Congelamiento (Fase 2):** Máximo 4 por año calendario. Mínimo 7 días por evento. `EndDate` se extiende automáticamente.
7. **Cancelación (Fase 2):** Acceso residual hasta `EndDate` original. Sin reembolsos automáticos.
---
## 5. Seguridad
- JWT en memoria. Refresh Token en **HttpOnly Cookie** exclusivamente.
- No almacenar tokens en `localStorage` ni en `sessionStorage`.
- Todo endpoint protegido requiere autorización RBAC: `Owner`, `Admin`, `Receptionist`, `Trainer`, `Member`.
---
## 6. Migraciones de Base de Datos
- Prohibido modificar el schema sin una migración EF Core formal (nombre en PascalCase descriptivo).
- Las migraciones se aplican en CI/CD, no manualmente en producción.
---
## 7. Flujo SDD antes de escribir código
```
1. Validar la tarea contra PRD + User Stories
2. Si hay cambio estructural → actualizar RFC_001
3. Definir DTOs / contratos de API primero
4. Implementar en orden: Batch A (Dominio + DB) → Batch B (Lógica + API) → Batch C (UI + Offline)
5. Verificar que no se rompió ninguna regla de negocio de la sección 4
```
---
## 8. Mapa del Proyecto
Referencias rápidas para orientarse en el codebase:
- [Estructura de carpetas](docs/technical/folder-structure.md)
- [Modelos de dominio](docs/technical/domain-models.md)
- [Schema de base de datos](docs/technical/database-schema.md)
- [Estado de implementación por HU](docs/technical/implementation-status.md)
- [Patrones de implementación](docs/technical/patterns.md)



- **Rol:** Eres el Orquestador (Jarvis). Yo soy el Director (Tony Stark) [9].
- **Restricción:** Tienes **PROHIBIDO programar** directamente. Debes delegar en sub-agentes efímeros [10, 11].
- **Memoria:** Toda decisión técnica debe persistirse en **Engram** bajo el formato What/Why/Where/Learned [12, 13].
- **Flujo:** Sigue estrictamente el pipeline SDD: Exploración -> Propuesta -> Diseño -> Tareas -> Implementación (TDD) [14, 15].

