# Arquitectura — GymFlow Lite

## Capas del Backend (Clean Architecture)

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                    WebAPI (Presentation)                                    │
│  Controllers · Filters · Middleware · Program.cs                           │
│  AccessController · MembersController · IdempotencyFilter                  │
└──────────────────────────────┬──────────────────────────────────────────────┘
                               │ depende de ↓
┌──────────────────────────────▼──────────────────────────────────────────────┐
│                  Application (Use Cases)                                   │
│  UseCases · DTOs · Validators · Interfaces                                │
│  ValidateAccessUseCase · RegisterMemberUseCase                            │
│  IPhotoStorageService · CreateMemberValidator                             │
└─────────────────────┬───────────────────────────────┬───────────────────────┘
                      │ depende de ↓                 │ depende de ↓
┌─────────────────────▼──────────────────┐   ┌──────────────────────────────────┐
│          Domain (Entities)             │   │    Infrastructure (Impl)         │
│  Member · AccessLog                    │   │  GymFlowDbContext · Repos        │
│  MemberStatus (enum)                   │   │  MemberRepository                │
│  IMemberRepository                     │   │  AccessLogRepository             │
│  IAccessLogRepository                  │   │  LocalPhotoStorageService        │
└────────────────────────────────────────┘   └──────────────────────────────────┘
                                             │
                             ┌──────────────▼───────────────┐
                             │   PostgreSQL / EF Core       │
                             └──────────────────────────────┘
```

### Responsabilidades por capa

| Capa            | Responsabilidad                                   | Regla                                        |
|------------------|--------------------------------------------------|---------------------------------------------|
| **Domain**      | Entidades, value objects, interfaces de repo      | Sin dependencias externas. Pura lógica      |
| **Application** | Casos de uso, DTOs, validadores, interfaces       | No referencia EF Core ni frameworks         |
| **Infrastructure** | Repos y servicios externos                     | Puede referenciar EF Core, file system, etc.|
| **WebAPI**      | HTTP controllers, filtros, DI, middleware         | Coordina Application. Sin lógica de dominio |

---

## Flujo de una Request — POST /api/access/checkin

```plaintext
Cliente (React PWA)
  │  POST /api/access/checkin
  │  Headers: Authorization: Bearer <jwt>, X-Client-Guid: <uuid>
  │  Body: { memberId, clientGuid, performedByUserId }
  ↓
IdempotencyFilter
  ├─ ¿ClientGuid ya existe en AccessLogs? → 200 OK (no reprocesa)
  └─ No existe → continúa
  ↓
AccessController.CheckIn()
  │  Valida JWT y rol (Receptionist | Admin)
  ↓
ValidateAccessUseCase.ExecuteAsync()
  ├─ Paso 1: GetByClientGuidAsync() → ¿ya procesado? → retorna resultado original
  ├─ Paso 2: GetByIdAsync(memberId) → ¿existe el socio?
  ├─ Paso 3: member.CanAccess() → ¿Active + fecha vigente?
  └─ Paso 4: AccessLog.Create() → AddAsync()
  ↓
Result<AccessValidationDto>
  ↓
AccessController → HTTP Response (200 OK | 403 Forbidden | 404 Not Found)
  ↓
Cliente
  ├─ 200 OK → rehidratar IndexedDB store `users`
  └─ 403    → mostrar MemberAccessCard en rojo
```

---

## Esquema IndexedDB (Dexie.js v3.x)

Definido en `src/frontend/src/db/gymflow.db.ts`.

### Store: `users`

| Campo             | Tipo                    | Descripción                                    |
|-------------------|-------------------------|------------------------------------------------|
| `id`              | `string` (PK)          | GUID del socio — corresponde al backend        |
| `fullName`        | `string`               | Nombre completo del socio                      |
| `photoWebP`       | `string`               | Data URI de foto en WebP                       |
| `status`          | `'Active' | 'Frozen' | 'Expired'` | Estado de la membresía                      |
| `membershipEndDate` | `string`             | Fecha ISO (`'YYYY-MM-DD'`) final de membresía  |

### Stores adicionales:
- **`sync_queue`**: Para eventos locales a sincronizar con el servidor.
- **`error_queue`**: Almacena intentos de sincronización fallidos.
- **`products`**, **`sales`**, **`payments`**: Manejan inventarios, transacciones y pagos.
- **`measurements`**: Registra métricas físicas de los miembros.
- **`exercise_catalog`**, **`routines`**, **`routine_assignments`**: Gestión de ejercicios y asignaciones.
- **`workout_logs`**: Historial de entrenamientos.

---

## Estrategia de Inicialización de la Base de Datos

En entornos de desarrollo, la inicialización utiliza el método `EnsureCreatedAsync()` para garantizar que las tablas sean creadas en ausencia de migraciones. Este enfoque simplifica el setup inicial pero no debe usarse en producción, donde las migraciones de Entity Framework Core son fundamentales.

---

## Referencias

| Archivo                                      | Descripción                                      |
|---------------------------------------------|-------------------------------------------------|
| `Infrastructure/Services/LocalPhotoStorageService.cs` | Implementación local de IPhotoStorageService |
| `Domain/Entities/Member.cs`                 | Reglas de acceso para miembros                   |
| `frontend/src/db/gymflow.db.ts`             | Definiciones de IndexedDB con Dexie.js          |