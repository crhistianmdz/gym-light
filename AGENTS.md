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
/docs/User_Stories_GymFlow.md             ← Criterios de aceptación por HU
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

## 3. Estructura de Directorios (a crear)

```
/src/backend/     → Solución .NET 8 (Clean Architecture)
  /Domain/        → Entidades, Value Objects, interfaces de repositorio
  /Application/   → Use Cases, DTOs, interfaces de servicios
  /Infrastructure/→ EF Core, repositorios, servicios externos
  /WebAPI/        → Controllers, Filters, Middleware

/src/frontend/    → App React + Vite
  /src/db/        → Esquema Dexie.js (stores: users, sync_queue, metadata)
  /src/services/  → Lógica de red y sincronización
  /src/components/→ Componentes Material Design

/docker/          → docker-compose.dev.yml, docker-compose.prod.yml
/docs/            → PRD, RFC, User Stories (solo lectura para agentes)
```

---

## 4. Comandos (una vez implementado el proyecto)

### Backend (.NET 8)

```bash
# Build
dotnet build src/backend/GymFlow.sln

# Tests
dotnet test src/backend/GymFlow.sln
dotnet test src/backend/GymFlow.sln --filter "FullyQualifiedName~MembershipService"

# Run dev
dotnet run --project src/backend/WebAPI

# Migrations
dotnet ef migrations add <NombreMigracion> --project src/backend/Infrastructure
dotnet ef database update --project src/backend/Infrastructure
```

### Frontend (React + Vite)

```bash
# Instalar dependencias
npm install

# Desarrollo
npm run dev

# Build de producción
npm run build

# Tests (Vitest esperado)
npm test
npm test -- --reporter=verbose src/frontend/src/services/sync.test.ts

# Lint
npm run lint
```

### Docker

```bash
docker compose -f docker/docker-compose.dev.yml up -d
docker compose -f docker/docker-compose.dev.yml down
```

---

## 5. Convenciones de Código

### Backend — C# / .NET 8

- **Naming:** PascalCase para clases, métodos y propiedades. camelCase para variables locales y parámetros.
- **DTOs:** Sufijo `Dto` (e.g. `MemberDto`, `CheckInRequestDto`). Definidos en la capa `Application`.
- **Interfaces:** Prefijo `I` (e.g. `IMemberRepository`, `ISyncService`).
- **Async:** Todos los métodos de I/O deben ser `async Task<T>`. Sufijo `Async` obligatorio.
- **Validación:** FluentValidation en la capa `Application`. Nunca validar en controllers.
- **Errores:** Usar `Result<T>` pattern o excepciones de dominio tipadas. No lanzar `Exception` genérica.
- **Idempotencia:** Todo endpoint que reciba datos de la cola de sync debe aceptar `ClientGuid` (UUID v4) y estar protegido por `IdempotencyFilter`.

```csharp
// ✅ Correcto
public async Task<Result<MemberDto>> GetMemberByIdAsync(Guid memberId, CancellationToken ct)

// ❌ Incorrecto
public MemberDto GetMember(int id)
```

### Frontend — React / TypeScript

- **Naming:** PascalCase para componentes. camelCase para funciones, variables y hooks. UPPER_SNAKE_CASE para constantes.
- **Archivos:** Un componente por archivo. El nombre del archivo = nombre del componente (e.g. `CheckInPanel.tsx`).
- **Tipos:** TypeScript estricto (`strict: true`). Prohibido `any`. Usar `unknown` + type guard si es necesario.
- **Imports:** Absolutos con alias `@/` para `src/`. Orden: librerías externas → internos de `@/` → relativos → tipos.
- **Hooks:** Custom hooks en `src/hooks/`. Prefijo `use` obligatorio.
- **Estado local:** `useState` / `useReducer`. Para estado global/server, definir estrategia en RFC antes de implementar.
- **Componentes:** Funcionales siempre. Sin class components.

```tsx
// ✅ Correcto
import { useState } from 'react'
import type { Member } from '@/types/member'

// ❌ Incorrecto
import * as React from 'react'
const data: any = {}
```

---

## 6. Reglas de Negocio — Prohibido Ignorarlas

Estas reglas vienen del PRD y la RFC. Cualquier código que las viole es incorrecto por definición:

1. **Acceso:** Denegar si `status === 'Frozen' || status === 'Expired'`. Sin excepciones.
2. **Foto obligatoria:** El botón de guardado de socio debe estar deshabilitado si no hay foto. La imagen se convierte a **WebP** en el cliente antes de enviar o cachear.
3. **Autoridad del servidor:** En conflicto de datos local vs. nube → el servidor gana. El cliente sobreescribe su caché.
4. **ClientGuid:** Toda escritura local genera un `UUID v4`. El backend responde `200 OK` si ya procesó ese GUID (no reprocesa).
5. **Stock:** Bloquear venta si stock local es `0`. Mostrar alerta crítica si stock `<= 20%`.
6. **Congelamiento (Fase 2):** Máximo 4 por año calendario. Mínimo 7 días por evento. `EndDate` se extiende automáticamente.
7. **Cancelación (Fase 2):** Acceso residual hasta `EndDate` original. Sin reembolsos automáticos.

---

## 7. Persistencia Local (Dexie.js)

```typescript
// Esquema obligatorio — definido en RFC 001
const db = new Dexie('gymflow')
db.version(1).stores({
  users:      'id, status, membershipEndDate',
  sync_queue: 'guid, type, timestamp',
  metadata:   'key',
})
```

- **Prohibido `localStorage`** para datos de socios o transacciones. Solo IndexedDB.
- Solicitar `navigator.storage.persist()` al inicializar la app (HU-05).
- Limpiar `sync_queue` únicamente tras confirmación exitosa del servidor.

---

## 8. Service Worker — Estrategia Network-First

```
Request API
  ├─ Online → fetch() → actualizar store `users` → responder
  └─ Offline / timeout >2s → consultar IndexedDB → encolar en sync_queue → responder
```

- Retry automático cada **5 minutos** o al evento `online`.
- Si un registro falla **3 veces** → moverlo a "Bandeja de Errores" para revisión manual.
- Header `X-Data-Version` en cada respuesta: si es mayor a `metadata.dataVersion`, limpiar IndexedDB y re-descargar snapshot.

---

## 9. Seguridad

- JWT en memoria. Refresh Token en **HttpOnly Cookie** exclusivamente.
- No almacenar tokens en `localStorage` ni en `sessionStorage`.
- Todo endpoint protegido requiere autorización RBAC: `Owner`, `Admin`, `Receptionist`, `Trainer`, `Member`.

---

## 10. Errores y Red

- **Todo** código que haga I/O (red o IndexedDB) debe tener `try/catch` con fallback offline explícito.
- No subir código con errores de red silenciados (`catch (e) {}`).
- El indicador de sincronización en la UI debe reflejar: Verde (sincronizado) / Naranja (pendientes) / Gris (offline).

---

## 11. Migraciones de Base de Datos

- **Prohibido** modificar el esquema de la base de datos sin crear una migración EF Core formal.
- Nombre de migración en PascalCase descriptivo: `AddFreezeCountToMembership`, `CreateSyncAuditTable`.
- Las migraciones se aplican en CI/CD, no manualmente en producción.

---

## 12. Flujo SSD antes de escribir código

```
1. Validar la tarea contra PRD + User Stories
2. Si hay cambio estructural → actualizar RFC_001
3. Definir DTOs / contratos de API primero
4. Implementar en orden: Batch A (Dominio + DB) → Batch B (Lógica + API) → Batch C (UI + Offline)
5. Verificar que no se rompió ninguna regla de negocio de la sección 6
```

---

## 13. Mapa del Proyecto

Referencias rápidas para orientarse en el codebase:

- [Estructura de carpetas](docs/technical/folder-structure.md)
- [Modelos de dominio](docs/technical/domain-models.md)
- [Schema de base de datos](docs/technical/database-schema.md)
- [Estado de implementación por HU](docs/technical/implementation-status.md)
- [Patrones de implementación](docs/technical/patterns.md)
