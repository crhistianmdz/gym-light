# HU-08 — Cancelación con Acceso Residual

## Overview

La HU-08 introduce la cancelación de membresías en la Fase 2 del MVP.
Permite que un socio (o Admin/Owner en su nombre) marque su membresía para **no renovarse**, conservando acceso residual hasta `MembershipEndDate` original sin reembolsos automáticos.

Características clave:

- La cancelación **no bloquea el acceso** — el socio sigue entrando hasta que su `MembershipEndDate` venza.
- Si el socio está `Frozen`, la cancelación se aplica pero el bloqueo por congelamiento se mantiene.
- No se puede cancelar una membresía `Expired` — retorna `400 BadRequest`.
- Operación **idempotente** via `ClientGuid` — doble envío retorna `200 OK` sin reprocesar.
- Compatible con el flujo offline-first: se encola en `sync_queue` si no hay conexión.

---

## Reglas de Negocio

| Regla | Descripción | Fuente |
|---|---|---|
| R1 | El socio conserva acceso residual hasta `MembershipEndDate` original | PRD §3 Fase 2, HU-08 CA-1 |
| R2 | No se generan reembolsos automáticos | PRD §3 Fase 2, HU-08 CA-2 |
| R3 | `Active` → `Status = Cancelled`, `AutoRenewEnabled = false`, `CancelledAt = UtcNow` | Diseño de dominio |
| R4 | `Frozen` → mantiene `Status = Frozen`, `AutoRenewEnabled = false`, `CancelledAt = UtcNow` | Edge case Frozen+Cancelled |
| R5 | `Expired` → `DomainException` → `400 BadRequest` | Cancelar membresía vencida es inválido |
| R6 | `Member` solo puede cancelar su propio id; `Admin/Owner` pueden cancelar cualquiera | RBAC |
| R7 | `Receptionist` y `Trainer` reciben `403 Forbidden` | RBAC |
| R8 | `Cancelled` no bloquea `CanAccess()` — el acceso lo controlan `Frozen` y `Expired` | Invariante de dominio |

---

## Architecture

### Backend — Clean Architecture

```
Domain
  └── Enums/MemberStatus.cs             ← Agregado Cancelled = 3
  └── Entities/Member.cs                ← Nuevas props AutoRenewEnabled, CancelledAt
                                           Nuevo método Cancel()
                                           CanAccess() actualizado para incluir Cancelled

Application
  └── DTOs/CancelMembershipRequestDto.cs ← Request: { clientGuid }
  └── DTOs/MemberDto.cs                  ← Extendido con AutoRenewEnabled, CancelledAt
  └── UseCases/Members/CancelMembershipUseCase.cs

Infrastructure
  └── Persistence/Migrations/AddAutoRenewToMember.cs ← AutoRenewEnabled BIT DEFAULT 1, CancelledAt DATETIME2

WebAPI
  └── Controllers/MembersController.cs  ← POST /{id}/cancel con [AllowAnonymous] + check manual
  └── Extensions/AuthExtensions.cs      ← DI de CancelMembershipUseCase
```

### State Machine de `Cancel()`

```
Active   ──Cancel()──→ Status = Cancelled
                        AutoRenewEnabled = false
                        CancelledAt = UtcNow

Frozen   ──Cancel()──→ Status = Frozen  (sin cambio)
                        AutoRenewEnabled = false
                        CancelledAt = UtcNow

Expired  ──Cancel()──→ DomainException → 400 BadRequest
```

### `CanAccess()` — Regla actualizada

```csharp
// Active o Cancelled con EndDate vigente → acceso permitido
// Frozen o Expired → deniegan siempre
public bool CanAccess() =>
    (Status == MemberStatus.Active || Status == MemberStatus.Cancelled)
    && MembershipEndDate >= DateOnly.FromDateTime(DateTime.UtcNow);
```

### Flujo `CancelMembershipUseCase`

```
1. RBAC self-check
   - si callerRole == "Member" && callerId != memberId → Forbidden (403)
   - si callerRole == "Receptionist" || "Trainer" → Forbidden (403)
       ↓
2. Verificar que el socio existe → 404 si no
       ↓
3. Idempotencia: si AutoRenewEnabled == false && CancelledAt != null → 200 OK sin reprocesar
       ↓
4. member.Cancel() → capturar DomainException (Expired) → 400
       ↓
5. memberRepo.UpdateAsync()
       ↓
6. Retornar Result<MemberDto>.Success(memberDto)
```

### Decisión RBAC — `[AllowAnonymous]` en el endpoint

El controlador tiene `[Authorize(Roles = "Receptionist,Admin,Owner")]` a nivel de clase, lo que excluye al rol `Member`. Para no romper los endpoints existentes, el endpoint `/cancel` usa `[AllowAnonymous]` y realiza la validación de identidad y rol manualmente:

```csharp
[HttpPost("{id:guid}/cancel")]
[AllowAnonymous]
public async Task<IActionResult> CancelMembership(...)
{
    var callerId = GetCurrentUserId();
    if (callerId is null) return Unauthorized();  // sin JWT → 401

    var callerRole = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
    if (callerRole is "Receptionist" or "Trainer") return Forbid();  // → 403

    // UseCase valida isSelf || isAdminOrOwner
}
```

> ⚠️ **Deuda técnica**: implementar una policy `SelfOrAdminPolicy` formal antes de HU-09 para evitar el patrón `[AllowAnonymous]` + validación manual.

---

## Database

### Modificación en tabla `Members`

| Columna | Tipo | Default | Descripción |
|---|---|---|---|
| `AutoRenewEnabled` | `BIT NOT NULL` | `1` | `false` cuando la membresía fue cancelada |
| `CancelledAt` | `DATETIME2 NULL` | `NULL` | Fecha UTC en que se aplicó la cancelación |

**Migración zero-downtime**: el `DEFAULT 1` garantiza que todos los registros existentes queden con `AutoRenewEnabled = true` antes del redeploy de la aplicación.

### Migración

```bash
dotnet ef migrations add AddAutoRenewToMember \
  --project src/backend/Infrastructure \
  --startup-project src/backend/WebAPI

dotnet ef database update \
  --project src/backend/Infrastructure
```

### Por qué flag y no solo enum

Se eligió `AutoRenewEnabled + CancelledAt` en lugar de solo `MemberStatus.Cancelled` para:
1. Representar el estado compuesto `Frozen + Cancelled` sin perder información.
2. Separar semántica: `Status` controla acceso operativo; `AutoRenewEnabled` controla política de renovación.
3. Facilitar reportes futuros (HU-12) con `CancelledAt` como timestamp auditable.

---

## API Reference

Ver sección **HU-08** en [`api-reference.md`](./api-reference.md).

---

## Frontend

### Tipos (`src/frontend/src/types/cancel.ts`)

```typescript
interface CancelMembershipRequest {
  clientGuid: string   // UUID v4 generado con crypto.randomUUID()
}

interface CancelMembershipResult {
  id: string
  fullName: string
  photoWebPUrl: string
  status: 'Active' | 'Frozen' | 'Expired' | 'Cancelled'
  membershipEndDate: string    // 'YYYY-MM-DD'
  autoRenewEnabled: boolean
  cancelledAt: string | null   // ISO 8601 UTC
}
```

### Servicio (`src/frontend/src/services/cancelService.ts`)

| Función | Descripción |
|---|---|
| `cancelMembership(memberId)` | Network-first. Si offline: encola en `sync_queue` y actualiza `db.users` optimistamente |

Flujo:
```
cancelMembership(memberId)
  ├─ Online  → POST /api/members/{id}/cancel
  │            → actualizar db.users con respuesta del servidor
  └─ Offline → sync_queue.add({ guid, type: 'MemberUpdate', action: 'cancel' })
               → db.users.update({ autoRenewEnabled: false, status: 'Cancelled' })
               → throw 'OFFLINE_QUEUED'
```

3 fallos consecutivos → mover a `error_queue` para revisión manual.

### Componentes (`src/frontend/src/components/MemberCancel/`)

#### `CancelMembershipPanel`

Panel para socio (Member) y Admin/Owner que adapta su UI según el estado actual:

| Estado del socio | UI mostrada |
|---|---|
| `Active` con `autoRenewEnabled: true` | Botón "Cancelar membresía" habilitado → modal de confirmación |
| `Frozen` con `autoRenewEnabled: true` | Botón habilitado + alert "La membresía está congelada. La cancelación se registrará pero el acceso seguirá bloqueado hasta el fin del período de pausa." |
| `Expired` | Alert de advertencia, botón deshabilitado |
| `Cancelled` (`autoRenewEnabled: false`) | Alert info "Membresía cancelada. Acceso residual hasta {fecha}." |
| `Frozen` + `autoRenewEnabled: false` | Alert info "Membresía congelada con cancelación activa. Acceso bloqueado hasta {fecha}." |
| `OFFLINE_QUEUED` | Alert warning "Cancelación pendiente de sincronización." |

**Modal de confirmación:**
```
¿Cancelar membresía?

El acceso al gimnasio se mantendrá hasta el {membershipEndDate}.
No se generarán reembolsos automáticos.

[Cancelar membresía]   [Volver]
```

```tsx
// Uso
import { CancelMembershipPanel } from '@/components/MemberCancel'

<CancelMembershipPanel
  memberId={member.id}
  memberStatus={member.status}
  membershipEndDate={member.membershipEndDate}
  autoRenewEnabled={member.autoRenewEnabled}
  onSuccess={() => refetchMember()}
/>
```

---

## Security

| Endpoint | Roles permitidos | Restricción adicional |
|---|---|---|
| `POST /api/members/{id}/cancel` | `Member`, `Admin`, `Owner` | `Member` solo puede cancelar su propio id |

| Rol | Resultado |
|---|---|
| `Member` (self) | `200 OK` |
| `Member` (otro id) | `403 Forbidden` |
| `Admin` / `Owner` | `200 OK` |
| `Receptionist` / `Trainer` | `403 Forbidden` |
| Sin JWT | `401 Unauthorized` |

---

## Tests

### Backend — Unit Tests (`Tests/Domain/MemberCancelTests.cs`)

| Test | Descripción |
|---|---|
| `Cancel_WhenActive_SetsStatusCancelledAndFlags` | Status → Cancelled, AutoRenewEnabled = false, CancelledAt != null |
| `Cancel_WhenFrozen_KeepsFrozenStatusButSetsFlags` | Status sigue Frozen, flags se aplican |
| `Cancel_WhenExpired_ThrowsDomainException` | DomainException para membresía vencida |
| `CanAccess_WhenCancelledWithValidEndDate_ReturnsTrue` | Acceso residual activo |
| `CanAccess_WhenCancelledWithExpiredEndDate_ReturnsFalse` | Sin acceso tras EndDate |

### Backend — Integration Tests (`Tests/Application/UseCases/CancelMembershipUseCaseTests.cs`)

| Test | Descripción |
|---|---|
| `ExecuteAsync_AsAdmin_ReturnsOk` | Admin puede cancelar cualquier socio |
| `ExecuteAsync_AsOwner_ReturnsOk` | Owner puede cancelar cualquier socio |
| `ExecuteAsync_AsMemberSelf_ReturnsOk` | Member puede auto-cancelar |
| `ExecuteAsync_AsMemberOther_ReturnsForbidden` | Member no puede cancelar id ajeno |
| `ExecuteAsync_WhenExpired_ReturnsBadRequest` | Membresía vencida → 400 |
| `ExecuteAsync_WhenFrozen_CancelsWithoutChangingStatus` | Frozen + Cancel coexisten |
| `ExecuteAsync_DuplicateRequest_ReturnsOkWithoutCallingUpdate` | Idempotencia — no llama UpdateAsync |
| `ExecuteAsync_WhenMemberNotFound_Returns404` | Member inexistente → 404 |

---

## Files Changed

### Backend

| Archivo | Tipo de cambio |
|---|---|
| `Domain/Enums/MemberStatus.cs` | **Modificado** — `Cancelled = 3` agregado |
| `Domain/Entities/Member.cs` | **Modificado** — `AutoRenewEnabled`, `CancelledAt`, `Cancel()`, `CanAccess()` |
| `Application/DTOs/CancelMembershipRequestDto.cs` | **Nuevo** |
| `Application/DTOs/MemberDto.cs` | **Modificado** — `AutoRenewEnabled`, `CancelledAt` |
| `Application/UseCases/Members/CancelMembershipUseCase.cs` | **Nuevo** |
| `Application/Common/Result.cs` | **Corregido** — bug de llave de cierre + `IsFailure` + `Failure(msg, code)` |
| `Infrastructure/Migrations/AddAutoRenewToMember.cs` | **Nuevo** |
| `WebAPI/Controllers/MembersController.cs` | **Modificado** — `POST /{id}/cancel`, DI en constructor |
| `WebAPI/Extensions/AuthExtensions.cs` | **Modificado** — DI de `CancelMembershipUseCase` |
| `Tests/Domain/MemberCancelTests.cs` | **Nuevo** — 5 casos unitarios |
| `Tests/Application/UseCases/CancelMembershipUseCaseTests.cs` | **Nuevo** — 8 casos de integración |

### Frontend

| Archivo | Tipo de cambio |
|---|---|
| `src/frontend/src/types/cancel.ts` | **Nuevo** |
| `src/frontend/src/services/cancelService.ts` | **Nuevo** — network-first + offline |
| `src/frontend/src/components/MemberCancel/CancelMembershipPanel.tsx` | **Nuevo** — 5 estados de UI |
| `src/frontend/src/components/MemberCancel/index.ts` | **Nuevo** — barrel |
