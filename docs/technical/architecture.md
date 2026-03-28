# Arquitectura — GymFlow Lite

## Capas del Backend (Clean Architecture)

```
┌─────────────────────────────────────────────────────────┐
│                    WebAPI (Presentation)                 │
│  Controllers · Filters · Middleware · Program.cs         │
│  AccessController · MembersController · IdempotencyFilter│
└────────────────────────┬────────────────────────────────┘
                         │ depende de ↓
┌────────────────────────▼────────────────────────────────┐
│                  Application (Use Cases)                 │
│  UseCases · DTOs · Validators · Interfaces               │
│  ValidateAccessUseCase · RegisterMemberUseCase           │
│  IPhotoStorageService · CreateMemberValidator            │
└────────┬─────────────────────────────┬──────────────────┘
         │ depende de ↓                │ depende de ↓
┌────────▼─────────────┐   ┌───────────▼────────────────── ┐
│   Domain (Entities)  │   │    Infrastructure (Impl)       │
│  Member · AccessLog  │   │  GymFlowDbContext · Repos      │
│  MemberStatus (enum) │   │  MemberRepository              │
│  IMemberRepository   │   │  AccessLogRepository           │
│  IAccessLogRepository│   │  LocalPhotoStorageService      │
└──────────────────────┘   └────────────────────────────────┘
                                        │
                            ┌───────────▼──────────┐
                            │   PostgreSQL / EF Core│
                            └──────────────────────┘
```

### Responsabilidades por capa

| Capa | Responsabilidad | Regla |
|---|---|---|
| **Domain** | Entidades, value objects, interfaces de repositorio | Sin dependencias externas. Pura lógica de negocio |
| **Application** | Use Cases, DTOs, validadores, interfaces de servicios | No referencia EF Core ni frameworks. Solo Domain |
| **Infrastructure** | Implementaciones concretas de repositorios y servicios externos | Referencia EF Core, file system, etc. |
| **WebAPI** | HTTP controllers, filtros, DI, middleware | Orquesta Application. No tiene lógica de negocio |

---

## Flujo de una Request — POST /api/access/checkin

```
Cliente (React PWA)
  │  POST /api/access/checkin
  │  Headers: Authorization: Bearer <jwt>, X-Client-Guid: <uuid>
  │  Body: { memberId, clientGuid, performedByUserId }
  ▼
IdempotencyFilter
  ├─ ¿ClientGuid ya existe en AccessLogs? → 200 OK (no reprocesa)
  └─ No existe → continúa
  ▼
AccessController.CheckIn()
  │  Valida JWT y rol (Receptionist | Admin)
  ▼
ValidateAccessUseCase.ExecuteAsync()
  ├─ Paso 1: GetByClientGuidAsync() → ¿ya procesado? → retorna resultado original
  ├─ Paso 2: GetByIdAsync(memberId) → ¿existe el socio?
  ├─ Paso 3: member.CanAccess() → ¿Active + fecha vigente?
  └─ Paso 4: AccessLog.Create() → AddAsync()
  ▼
Result<AccessValidationDto>
  ▼
AccessController → HTTP Response (200 OK | 403 Forbidden | 404 Not Found)
  ▼
Cliente
  ├─ 200 OK → rehidratar IndexedDB store `users`
  └─ 403    → mostrar MemberAccessCard en rojo
```

---

## Esquema IndexedDB (Dexie.js v1)

Definido en `src/frontend/src/db/gymflow.db.ts`.

### Store: `users`

| Campo | Tipo | Descripción |
|---|---|---|
| `id` | `string` (PK) | GUID del socio — mismo que el backend |
| `fullName` | `string` | Nombre completo |
| `photoWebP` | `string` | Data URI WebP o URL relativa para verificación visual offline |
| `status` | `'Active' \| 'Frozen' \| 'Expired'` | Estado de la membresía |
| `membershipEndDate` | `string` | Fecha ISO `'YYYY-MM-DD'` |

### Store: `sync_queue`

| Campo | Tipo | Descripción |
|---|---|---|
| `guid` | `string` (PK) | UUID v4 — ClientGuid generado en el cliente |
| `type` | `'CheckIn' \| 'Sale' \| 'HealthUpdate'` | Tipo de evento |
| `payload` | `string` | JSON serializado de la transacción |
| `timestamp` | `number` | Unix timestamp en ms |
| `isOffline` | `boolean` | `true` si fue generado sin conexión |
| `retryCount` | `number` | Intentos fallidos — al llegar a 3 pasa a bandeja de errores |

### Store: `metadata`

| Campo | Tipo | Descripción |
|---|---|---|
| `key` | `string` (PK) | Clave de configuración |
| `value` | `string` | Valor serializado |

Claves usadas: `dataVersion`, `lastSyncTimestamp`

---

## Estrategia Offline-First — Network-First with Fallback

```
checkInMember(memberId)
  │
  ├─ Generar clientGuid = crypto.randomUUID()
  │
  ├─ Intentar fetch() con AbortController (timeout 2s)
  │    │
  │    ├─ ✅ Éxito (200 | 403)
  │    │    ├─ Parsear respuesta
  │    │    ├─ Rehidratar db.users con datos del servidor  ← Autoridad del Servidor
  │    │    └─ Retornar { source: 'online', ... }
  │    │
  │    └─ ❌ Error / Timeout
  │         │
  │         └─ validateOffline()
  │              ├─ db.users.get(memberId)         ← CA-1 HU-01
  │              ├─ Evaluar status + fecha localmente
  │              ├─ db.sync_queue.add({ guid, ... }) ← CA-3 HU-01
  │              └─ Retornar { source: 'offline', ... }
  │
  └─ SyncStatusBadge se actualiza en tiempo real (useLiveQuery)
```

### Notas operativas

- **Timeout**: `AbortController` + `setTimeout(2000ms)` — `fetch()` no tiene timeout nativo
- **Reintentos**: cada 5 min o al evento `window.online` (implementado en HU-04)
- **Bandeja de errores**: `retryCount >= 3` → requiere revisión manual (HU-04)
- **Limpieza de cola**: solo tras confirmación exitosa del servidor (nunca optimista)

---

## Referencias

| Archivo | Descripción |
|---|---|
| `Domain/Entities/Member.cs` | Reglas de acceso: `CanAccess()`, `GetDenialReason()` |
| `Application/UseCases/Access/ValidateAccessUseCase.cs` | Lógica de check-in + idempotencia |
| `WebAPI/Filters/IdempotencyFilter.cs` | Protección por header `X-Client-Guid` |
| `Infrastructure/Persistence/GymFlowDbContext.cs` | Índice `UNIQUE` sobre `AccessLog.ClientGuid` |
| `frontend/src/db/gymflow.db.ts` | Esquema Dexie completo + `initDatabase()` |
| `frontend/src/services/accessService.ts` | Network-First + fallback offline |
