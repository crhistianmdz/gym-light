# HU-09 — Perfil Antropométrico y Progreso

## Overview

La HU-09 introduce el módulo de medidas antropométricas en la Fase 2 del MVP.
Permite que **Entrenadores** y **Socios** (solo las propias) registren medidas físicas del socio, demostrando el valor del servicio y alimentando las gráficas de evolución (HU-10).

Características clave:

- **7 medidas obligatorias**: peso, % grasa corporal, pecho, cintura, cadera, brazo, pierna.
- **Doble sistema de unidades**: métrico (kg/cm) e imperial (lbs/inches) — los valores se almacenan tal como se ingresaron junto al `UnitSystem`.
- **Ownership enforcement**: el Socio solo puede registrar/ver sus propias medidas; Trainer/Admin/Owner acceden a las de cualquier socio.
- **Offline-first**: se encola en `sync_queue` con tipo `HealthUpdate` y se sincroniza automáticamente al recuperar la conexión.
- **Idempotencia** via `ClientGuid` — doble envío retorna `200 OK` sin duplicar registros.
- **Historial ordenado**: las medidas se listan por `RecordedAt` descendente (más reciente primero).

---

## Decisiones de PO (2026-03-30)

| # | Pregunta | Decisión |
|---|----------|----------|
| Q1 | ¿Quién puede crear medidas? | Trainer + Member (solo las propias) + Admin + Owner |
| Q2 | ¿Soporte offline? | Sí — Dexie store `measurements` + `HealthUpdate` en sync_queue |
| Q3 | ¿Campos obligatorios? | Todos (sin nullables en los 7 campos numéricos) |
| Q4 | ¿Unidades? | Ambas — métrico (kg/cm) e imperial (lbs/inches), campo `UnitSystem` en entidad |

---

## Reglas de Negocio

| Regla | Descripción | Fuente |
|---|---|---|
| R1 | Los 7 campos numéricos son obligatorios y deben ser > 0 | HU-09 CA-1, CA-3 |
| R2 | El campo `UnitSystem` se almacena junto a la toma — sin conversión server-side | HU-09 CA-3, CA-4, decisión PO Q4 |
| R3 | `Member` solo puede crear/ver medidas de su propio `memberId` | HU-09 CA-2, decisión PO Q1 |
| R4 | `Trainer`, `Admin`, `Owner` pueden crear/ver medidas de cualquier socio | HU-09 CA-7 |
| R5 | `Receptionist` recibe `403 Forbidden` en todos los endpoints | RBAC |
| R6 | `ClientGuid` único por registro — idempotencia a nivel BD (índice UNIQUE) | HU-09 CA-5, RFC-001 §Idempotencia |
| R7 | Offline: se encola como `HealthUpdate` en `sync_queue`; se sincroniza al reconectar | HU-09 CA-5, RFC-001 |
| R8 | Las medidas se retornan ordenadas por `RecordedAt` descendente | HU-09 CA-6 |

---

## Architecture

### Backend — Clean Architecture

```
Domain
  └── Enums/UnitSystem.cs                    ← Metric = 0, Imperial = 1
  └── Entities/BodyMeasurement.cs            ← Entidad con factory + validaciones
  └── Interfaces/IBodyMeasurementRepository.cs ← AddAsync, GetByMemberIdAsync, GetByClientGuidAsync

Infrastructure
  └── Persistence/GymFlowDbContext.cs        ← DbSet<BodyMeasurement> + EF mapping
  └── Persistence/Migrations/
  │     AddBodyMeasurementsTable.cs          ← Tabla + índices
  └── Persistence/Repositories/
        BodyMeasurementRepository.cs         ← Implementación EF Core

Application
  └── DTOs/BodyMeasurements/
  │     AddBodyMeasurementRequest.cs         ← Input DTO
  │     BodyMeasurementDto.cs                ← Response DTO
  └── Validators/AddBodyMeasurementValidator.cs ← FluentValidation todos los campos > 0
  └── UseCases/BodyMeasurements/
        AddBodyMeasurementUseCase.cs         ← Ownership + idempotencia + creación
        GetBodyMeasurementsUseCase.cs        ← Lista con ownership check

WebAPI
  └── Controllers/BodyMeasurementsController.cs ← POST + GET, RBAC [Trainer/Admin/Owner/Member]

Tests
  └── Domain/BodyMeasurementTests.cs         ← Unit tests de entidad
  └── UseCases/BodyMeasurements/
        AddBodyMeasurementUseCaseTests.cs    ← Idempotencia, RBAC, ownership
        GetBodyMeasurementsUseCaseTests.cs   ← RBAC, ownership
```

### Frontend — React + Vite PWA

```
src/
  types/measurement.ts                       ← MeasurementDto, AddMeasurementRequest, UnitSystem
  db/gymflow.db.ts                           ← MeasurementLocal + store measurements (v4)
  services/
    measurementService.ts                    ← addMeasurement (online/offline), getMeasurements
    syncService.ts                           ← HealthUpdate en ENDPOINT_MAP + updateLocalCache
  components/
    AnthropometryForm/AnthropometryForm.tsx  ← Form MUI, toggle métrico/imperial, 7 campos
    AnthropometryHistory/AnthropometryHistory.tsx ← Tabla MUI, historial con labels de unidades
  pages/MemberDetail/MemberDetail.tsx        ← Ruta /members/:id/anthropometry
  __tests__/measurementService.test.ts       ← Tests online/offline/dexie
```

---

## Data Model

### `BodyMeasurement` (backend entity)

| Campo | Tipo | Constraints | Descripción |
|---|---|---|---|
| `Id` | `Guid` | PK | Identificador único |
| `MemberId` | `Guid` | FK → Members, CASCADE | Socio al que pertenece la medida |
| `RecordedById` | `Guid` | FK → AppUsers | Usuario que registró la medida |
| `RecordedAt` | `DateTime` | UTC, NOT NULL | Fecha/hora de la toma |
| `WeightKg` | `decimal(7,2)` | > 0, NOT NULL | Peso (en la unidad del UnitSystem) |
| `BodyFatPct` | `decimal(5,2)` | > 0, NOT NULL | % de grasa corporal |
| `ChestCm` | `decimal(6,2)` | > 0, NOT NULL | Circunferencia de pecho |
| `WaistCm` | `decimal(6,2)` | > 0, NOT NULL | Circunferencia de cintura |
| `HipCm` | `decimal(6,2)` | > 0, NOT NULL | Circunferencia de cadera |
| `ArmCm` | `decimal(6,2)` | > 0, NOT NULL | Circunferencia de brazo |
| `LegCm` | `decimal(6,2)` | > 0, NOT NULL | Circunferencia de pierna |
| `UnitSystem` | `string` | NOT NULL | `"Metric"` o `"Imperial"` |
| `ClientGuid` | `string` | UNIQUE INDEX | Idempotencia cliente |
| `Notes` | `string?` | NULL | Observaciones opcionales |

> **Nota de unidades**: los campos `WeightKg`, `ChestCm`, etc. llevan el sufijo métrico por convención de naming, pero almacenan el valor tal como fue ingresado — si `UnitSystem = Imperial`, `WeightKg` contendrá el valor en lbs. La conversión es responsabilidad del frontend.

### DB Indexes

```sql
-- Idempotencia
CREATE UNIQUE INDEX IX_BodyMeasurements_ClientGuid ON BodyMeasurements(ClientGuid);

-- Soporte para HU-10 (gráficas por socio)
CREATE INDEX IX_BodyMeasurements_MemberId_RecordedAt ON BodyMeasurements(MemberId, RecordedAt);
```

### `MeasurementLocal` (Dexie — IndexedDB)

```typescript
interface MeasurementLocal {
  id?: number;           // autoincrement
  memberId: string;
  recordedById: string;
  recordedAt: string;    // ISO UTC
  weightKg: number;
  bodyFatPct: number;
  chestCm: number;
  waistCm: number;
  hipCm: number;
  armCm: number;
  legCm: number;
  unitSystem: 'metric' | 'imperial';
  notes?: string;
  clientGuid: string;
  syncStatus: 'pending' | 'synced' | 'error';
}
// Dexie store: '++id, memberId, clientGuid, recordedAt, syncStatus'
```

---

## API Endpoints

### POST `/api/members/{memberId}/measurements`

Registra una nueva toma de medidas antropométricas.

**Roles permitidos:** `Trainer`, `Admin`, `Owner`, `Member` (solo `memberId` propio)

#### Headers

| Header | Requerido | Descripción |
|---|---|---|
| `Authorization` | ✅ | `Bearer <jwt>` |
| `Content-Type` | ✅ | `application/json` |

#### Request Body

```json
{
  "clientGuid": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "recordedAt": "2026-03-30T20:00:00Z",
  "weightKg": 78.5,
  "bodyFatPct": 18.2,
  "chestCm": 95.0,
  "waistCm": 82.5,
  "hipCm": 98.0,
  "armCm": 35.0,
  "legCm": 58.0,
  "unitSystem": "Metric",
  "notes": "Después del ciclo de fuerza"
}
```

#### Responses

| Status | Descripción |
|---|---|
| `201 Created` | Medida registrada exitosamente |
| `200 OK` | `ClientGuid` ya procesado — retorna la medida original (idempotente) |
| `400 Bad Request` | Validación fallida (campo <= 0 o faltante) |
| `403 Forbidden` | Sin permisos (Receptionist, o Member intentando registrar para otro socio) |
| `404 Not Found` | Socio no encontrado |

#### Response Body (201 / 200)

```json
{
  "id": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "memberId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "recordedById": "b3d4e5f6-1234-5678-abcd-9876543210ab",
  "recordedAt": "2026-03-30T20:00:00Z",
  "weightKg": 78.5,
  "bodyFatPct": 18.2,
  "chestCm": 95.0,
  "waistCm": 82.5,
  "hipCm": 98.0,
  "armCm": 35.0,
  "legCm": 58.0,
  "unitSystem": "Metric",
  "notes": "Después del ciclo de fuerza",
  "clientGuid": "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
}
```

---

### GET `/api/members/{memberId}/measurements`

Obtiene el historial de medidas del socio, ordenado por fecha descendente.

**Roles permitidos:** `Trainer`, `Admin`, `Owner`, `Member` (solo `memberId` propio)

#### Headers

| Header | Requerido | Descripción |
|---|---|---|
| `Authorization` | ✅ | `Bearer <jwt>` |

#### Responses

| Status | Descripción |
|---|---|
| `200 OK` | Lista de medidas (puede ser vacía `[]`) |
| `403 Forbidden` | Sin permisos |
| `404 Not Found` | Socio no encontrado |

#### Response Body (200)

```json
[
  {
    "id": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
    "memberId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "recordedById": "b3d4e5f6-1234-5678-abcd-9876543210ab",
    "recordedAt": "2026-03-30T20:00:00Z",
    "weightKg": 78.5,
    "bodyFatPct": 18.2,
    "chestCm": 95.0,
    "waistCm": 82.5,
    "hipCm": 98.0,
    "armCm": 35.0,
    "legCm": 58.0,
    "unitSystem": "Metric",
    "notes": "Después del ciclo de fuerza",
    "clientGuid": "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
  }
]
```

---

## Offline Flow (Sync)

```
[AnthropometryForm] → measurementService.addMeasurement(memberId, request)
       │
       ├─ Online ──→ POST /api/members/{memberId}/measurements
       │             ← 201 BodyMeasurementDto
       │
       └─ Offline ──→ db.measurements.put({ ...request, syncStatus: 'pending' })
                    + db.sync_queue.add({
                        type: 'HealthUpdate',
                        payload: { memberId, ...request },
                        guid: request.clientGuid,
                        timestamp: Date.now(),
                        retryCount: 0
                      })

SyncService.processQueue() [al reconectar]
  └─ HealthUpdate → POST /api/members/{payload.memberId}/measurements
       └─ 201/200 → db.measurements.where('clientGuid').equals(guid).modify({ syncStatus: 'synced' })
```

---

## Frontend Components

### `AnthropometryForm`

| Prop | Tipo | Descripción |
|---|---|---|
| `memberId` | `string` | ID del socio al que se registran las medidas |
| `onSuccess?` | `() => void` | Callback tras registro exitoso |

**Comportamiento:**
- Toggle métrico/imperial actualiza labels en tiempo real
- Todos los campos required, validación > 0 con error helper
- Submit genera `crypto.randomUUID()` como `clientGuid`
- Snackbar de éxito/error
- Botón deshabilitado durante carga o si hay errores de validación

### `AnthropometryHistory`

| Prop | Tipo | Descripción |
|---|---|---|
| `memberId` | `string` | ID del socio cuyo historial se muestra |

**Comportamiento:**
- Fetch en mount via `measurementService.getMeasurements(memberId)`
- Fallback offline desde Dexie `measurements` store
- Loading skeleton mientras carga
- Empty state: "Sin medidas registradas"
- Columnas: Fecha, Peso, % Grasa, Pecho, Cintura, Cadera, Brazo, Pierna, Unidades, Notas
- Labels de unidades dinámicas según `unitSystem` de cada fila

---

## RBAC Summary

| Acción | Owner | Admin | Trainer | Receptionist | Member |
|---|---|---|---|---|---|
| `POST /measurements` | ✅ | ✅ | ✅ | ❌ 403 | ✅ solo propio |
| `GET /measurements` | ✅ | ✅ | ✅ | ❌ 403 | ✅ solo propio |

---

## Tests Coverage

| Capa | Archivo | Casos cubiertos |
|---|---|---|
| Domain | `BodyMeasurementTests.cs` | Create válido, cada campo <= 0, guids vacíos, Notes null |
| UseCase | `AddBodyMeasurementUseCaseTests.cs` | Idempotencia, ownership Member, RBAC Receptionist, Trainer ok |
| UseCase | `GetBodyMeasurementsUseCaseTests.cs` | RBAC, ownership, 404 |
| Frontend | `measurementService.test.ts` | Online POST, offline Dexie+sync_queue, get offline orden |
