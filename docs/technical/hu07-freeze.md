# HU-07 — Lógica de Congelamiento de Membresía

## Overview

La HU-07 introduce el módulo de congelamiento de membresías para la Fase 2 del MVP.
Permite a Admin y Owner pausar la suscripción de un socio por un período definido, con las siguientes garantías:

- Bloqueo de acceso **inmediato** al congelar (status → `Frozen`).
- Extensión automática de `MembershipEndDate` sumando los días de pausa.
- Validación de **mínimo 7 días** por evento.
- Límite de **máximo 4 congelamientos por año calendario** por socio.
- Al descongelar, el status vuelve a `Active` y el `EndDate` extendido **se mantiene**.

---

## Reglas de Negocio

| Regla | Descripción | Fuente |
|---|---|---|
| R1 | Máximo 4 congelamientos por año calendario | PRD §3 Fase 2, HU-07 CA-1 |
| R2 | Mínimo 7 días por evento | PRD §3 Fase 2, HU-07 CA-2 |
| R3 | Bloqueo de acceso inmediato al congelar | PRD §3 Fase 2, HU-07 CA-3 |
| R4 | `EndDate` se extiende sumando los días de pausa | PRD §3 Fase 2, HU-07 CA-3 |
| R5 | Solo `Admin` y `Owner` pueden congelar/descongelar | PRD §2 RBAC |
| R6 | Al descongelar, `EndDate` NO se revierte | Decisión de diseño — el socio "pagó" la extensión |

---

## Architecture

### Backend — Clean Architecture

```
Domain
  └── Entities/MembershipFreeze.cs        ← Entidad nueva; factory valida R2 (mín 7 días)
  └── Entities/Member.cs                  ← Métodos Freeze(days), Unfreeze(), CanFreezeThisYear(n)
  └── Interfaces/IMembershipFreezeRepository.cs

Application
  └── DTOs/FreezeMembershipDto.cs          ← Request: { memberId, startDate, endDate }
  └── DTOs/MembershipFreezeDto.cs          ← Response: freeze registrado
  └── Validators/FreezeMembershipValidator.cs
  └── UseCases/Members/FreezeMembershipUseCase.cs
  └── UseCases/Members/UnfreezeMembershipUseCase.cs

Infrastructure
  └── Persistence/GymFlowDbContext.cs      ← DbSet<MembershipFreeze> + configuración EF
  └── Persistence/Repositories/MembershipFreezeRepository.cs
  └── Persistence/Migrations/AddMembershipFreezeTable.cs

WebAPI
  └── Controllers/MembersController.cs    ← POST/{id}/freeze, DELETE/{id}/freeze, GET/{id}/freezes
  └── Extensions/AuthExtensions.cs        ← DI de los nuevos servicios
```

### Flujo `FreezeMembershipUseCase`

```
1. Validar inputs (FreezeMembershipValidator)
      ↓
2. Verificar que el socio existe y status == Active
      ↓
3. Contar freezes del año en curso → validar < 4 (R1)
      ↓
4. MembershipFreeze.Create() → valida mínimo 7 días (R2)
      ↓
5. member.Freeze(durationDays) → status = Frozen, EndDate += durationDays (R3 + R4)
      ↓
6. Persistir: freezeRepo.AddAsync() + memberRepo.UpdateAsync()
      ↓
7. Retornar MembershipFreezeDto
```

### Flujo `UnfreezeMembershipUseCase`

```
1. Verificar que el socio existe y status == Frozen
      ↓
2. Obtener congelamiento activo (StartDate <= hoy <= EndDate)
      ↓
3. member.Unfreeze() → status = Active (EndDate no se modifica — R6)
      ↓
4. Eliminar registro del congelamiento activo
      ↓
5. Persistir: memberRepo.UpdateAsync()
      ↓
6. Retornar MemberDto actualizado
```

---

## Database

### Tabla `MembershipFreezes`

| Columna | Tipo | Descripción |
|---|---|---|
| `Id` | `uuid` PK | Identificador del evento |
| `MemberId` | `uuid` FK → `Members` | Socio congelado |
| `StartDate` | `date` | Inicio del congelamiento (inclusive) |
| `EndDate` | `date` | Fin del congelamiento (inclusive) |
| `DurationDays` | `int` | Días efectivos (calculado al crear) |
| `CreatedByUserId` | `uuid` | Admin/Owner que aplicó el congelamiento |
| `CreatedAt` | `timestamptz` | Momento de creación (UTC) |

**FK:** `MemberId → Members(Id)` — `ON DELETE CASCADE`

**Índice:** `IX_MembershipFreezes_MemberId_StartDate` — optimiza las consultas por socio+año (R1).

### Modificación en `Member`

El método `Freeze()` sin parámetros fue **reemplazado** por `Freeze(int durationDays)`.
Cualquier código anterior que usara `member.Freeze()` sin argumentos **no compilará** — es intencional.

```csharp
// ✅ Correcto — HU-07
member.Freeze(freeze.DurationDays);  // extiende EndDate automáticamente

// ❌ Incorrecto — método obsoleto eliminado
member.Freeze();
```

### Migración

```bash
dotnet ef migrations add AddMembershipFreezeTable \
  --project src/backend/Infrastructure \
  --startup-project src/backend/WebAPI

dotnet ef database update \
  --project src/backend/Infrastructure
```

---

## API Reference

Ver sección **HU-07** en [`api-reference.md`](./api-reference.md).

---

## Frontend

### Tipos (`src/frontend/src/types/freeze.ts`)

```typescript
interface MembershipFreeze {
  id: string
  memberId: string
  startDate: string          // 'YYYY-MM-DD'
  endDate: string            // 'YYYY-MM-DD'
  durationDays: number
  createdByUserId: string
  createdAt: string
}

interface FreezeMembershipRequest {
  startDate: string
  endDate: string
}
```

### Servicio (`src/frontend/src/services/freezeService.ts`)

| Función | Descripción |
|---|---|
| `freezeMember(memberId, { startDate, endDate })` | Congela la membresía; actualiza IndexedDB con `status: 'Frozen'` |
| `unfreezeMember(memberId)` | Descongela; actualiza IndexedDB con status y endDate del servidor |
| `getFreezeHistory(memberId)` | Retorna historial completo de congelamientos |

Todas usan `fetchWithAuth` — manejan JWT automáticamente (refresh + retry en 401).

### Componentes (`src/frontend/src/components/MemberFreeze/`)

#### `FreezeMembershipPanel`

Panel Admin/Owner que adapta su UI según el estado del socio:

| Status | UI mostrada |
|---|---|
| `Active` | Formulario de fechas + botón "Congelar Membresía" |
| `Frozen` | Alert informativo + botón "Descongelar Membresía" |
| `Expired` | Alert de advertencia — no se puede congelar |

Validaciones cliente (espejo del servidor):
- `startDate >= hoy`
- `endDate > startDate`
- duración mínima 7 días

#### `FreezeHistoryList`

Tabla del historial de congelamientos con:
- Badge `N/4 este año` con color según proximidad al límite.
- Fila resaltada para el congelamiento activo (si existe).

```tsx
// Uso
import { FreezeMembershipPanel, FreezeHistoryList } from '@/components/MemberFreeze'

<FreezeMembershipPanel
  memberId={member.id}
  memberStatus={member.status}
  membershipEndDate={member.membershipEndDate}
  onSuccess={() => refetchMember()}
/>

<FreezeHistoryList memberId={member.id} />
```

### Integración con `CheckInPanel`

`MemberAccessCard` fue actualizado para mostrar un mensaje específico cuando `status === 'Frozen'`:

```
🚫 ACCESO DENEGADO
La membresía está congelada. El acceso está bloqueado hasta que finalice el período de pausa.
```

### Actualización de caché local (IndexedDB)

Ambas operaciones (congelar y descongelar) actualizan el store `users` de IndexedDB mediante el patrón **servidor autoritativo** (RFC §4 / PRD §4.3).
Esto garantiza que el check-in offline refleje el estado real de la membresía sin necesidad de re-sincronizar manualmente.

---

## Security

| Endpoint | Rol mínimo requerido |
|---|---|
| `POST /api/members/{id}/freeze` | `Admin`, `Owner` |
| `DELETE /api/members/{id}/freeze` | `Admin`, `Owner` |
| `GET /api/members/{id}/freezes` | `Admin`, `Owner` |

`Receptionist` recibe `403 Forbidden` en todos estos endpoints.

---

## Files Changed

### Backend

| Archivo | Tipo de cambio |
|---|---|
| `Domain/Entities/MembershipFreeze.cs` | **Nuevo** |
| `Domain/Entities/Member.cs` | **Modificado** — `Freeze(days)`, `Unfreeze()`, `CanFreezeThisYear(n)` |
| `Domain/Interfaces/IMembershipFreezeRepository.cs` | **Nuevo** |
| `Application/DTOs/FreezeMembershipDto.cs` | **Nuevo** |
| `Application/DTOs/MembershipFreezeDto.cs` | **Nuevo** |
| `Application/Validators/FreezeMembershipValidator.cs` | **Nuevo** |
| `Application/UseCases/Members/FreezeMembershipUseCase.cs` | **Nuevo** |
| `Application/UseCases/Members/UnfreezeMembershipUseCase.cs` | **Nuevo** |
| `Infrastructure/Persistence/GymFlowDbContext.cs` | **Modificado** — `DbSet<MembershipFreeze>` + EF config |
| `Infrastructure/Persistence/Repositories/MembershipFreezeRepository.cs` | **Nuevo** |
| `Infrastructure/Persistence/Migrations/AddMembershipFreezeTable.cs` | **Nuevo** |
| `WebAPI/Controllers/MembersController.cs` | **Modificado** — 3 endpoints nuevos |
| `WebAPI/Extensions/AuthExtensions.cs` | **Modificado** — DI actualizado |

### Frontend

| Archivo | Tipo de cambio |
|---|---|
| `src/types/freeze.ts` | **Nuevo** |
| `src/services/freezeService.ts` | **Nuevo** |
| `src/components/MemberFreeze/FreezeMembershipPanel.tsx` | **Nuevo** |
| `src/components/MemberFreeze/FreezeHistoryList.tsx` | **Nuevo** |
| `src/components/MemberFreeze/index.ts` | **Nuevo** |
| `src/components/CheckInPanel/MemberAccessCard.tsx` | **Modificado** — mensaje Frozen |
