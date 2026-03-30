# API Reference — GymFlow Lite

**Base URL:** `https://{host}/api`  
**Auth:** JWT en `Authorization: Bearer <token>` (obtenido vía HU-05)  
**Refresh Token:** HttpOnly Cookie `gymflow_refresh`

---

## POST /api/access/checkin

Registra el intento de acceso de un socio. Implementa idempotencia completa vía `ClientGuid`.

**Roles permitidos:** `Receptionist`, `Admin`

### Headers

| Header | Requerido | Descripción |
|---|---|---|
| `Authorization` | ✅ | `Bearer <jwt>` |
| `Content-Type` | ✅ | `application/json` |
| `X-Client-Guid` | Opcional | UUID v4 para cortocircuitar en `IdempotencyFilter` antes del handler |

> El `clientGuid` también va en el body. El header permite que el filtro detecte duplicados antes de llegar al Use Case.

### Request Body

```json
{
  "memberId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "clientGuid": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "performedByUserId": "9c8b7a65-4321-0fed-cba9-876543210fed"
}
```

| Campo | Tipo | Descripción |
|---|---|---|
| `memberId` | `Guid` | ID del socio a validar |
| `clientGuid` | `Guid` | UUID v4 generado en el cliente (`crypto.randomUUID()`) |
| `performedByUserId` | `Guid` | ID del recepcionista autenticado (trazabilidad HU-06) |

### Respuestas

#### 200 OK — Acceso permitido

```json
{
  "allowed": true,
  "member": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "fullName": "Juan Pérez",
    "photoWebPUrl": "/photos/3fa85f64-5717-4562-b3fc-2c963f66afa6.webp",
    "status": "Active",
    "membershipEndDate": "2026-12-31"
  },
  "denialReason": null
}
```

#### 403 Forbidden — Membresía vencida o congelada

```json
{
  "title": "Acceso denegado.",
  "detail": "La membresía está vencida.",
  "status": 403
}
```

#### 404 Not Found — Socio no encontrado

```json
{
  "title": "Socio 3fa85f64-... no encontrado.",
  "status": 404
}
```

#### 400 Bad Request — ClientGuid inválido en header

```json
{
  "title": "El header X-Client-Guid no es un GUID válido.",
  "status": 400
}
```

### Política de idempotencia

El sistema implementa tres capas de protección contra duplicados:

```
1. IdempotencyFilter     → lee X-Client-Guid del header → 200 OK inmediato si ya existe
2. ValidateAccessUseCase → GetByClientGuidAsync() antes de insertar
3. DB UNIQUE index       → AccessLogs.ClientGuid — última línea de defensa
```

Si el mismo `clientGuid` llega dos veces, el servidor responde `200 OK` con el resultado original sin reejecutar la lógica de negocio. El cliente puede limpiar su `sync_queue` de forma segura.

---

## POST /api/members

Registra un nuevo socio. La foto WebP es obligatoria.

**Roles permitidos:** `Receptionist`, `Admin`

### Request Body

```json
{
  "fullName": "Ana García",
  "photoWebPBase64": "data:image/webp;base64,UklGRlYA...",
  "membershipEndDate": "2027-03-01"
}
```

| Campo | Tipo | Validación |
|---|---|---|
| `fullName` | `string` | Requerido, máx 200 chars |
| `photoWebPBase64` | `string` | Requerido, debe comenzar con `data:image/webp;base64,` |
| `membershipEndDate` | `DateOnly` | Requerido, debe ser posterior a hoy |

### Respuestas

#### 201 Created

```json
{
  "id": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "fullName": "Ana García",
  "photoWebPUrl": "/photos/7c9e6679-7425-40de-944b-e07fc1f90ae7.webp",
  "status": "Active",
  "membershipEndDate": "2027-03-01"
}
```

`Location` header: `/api/members/7c9e6679-7425-40de-944b-e07fc1f90ae7`

#### 400 Bad Request — Validación fallida

```json
{
  "title": "Datos de registro inválidos.",
  "detail": "La foto del socio es obligatoria. | La fecha de vencimiento debe ser posterior a hoy.",
  "status": 400
}
```

---

## Curl de ejemplo

```bash
# Check-in
curl -X POST https://localhost:5001/api/access/checkin \
  -H "Authorization: Bearer $JWT" \
  -H "Content-Type: application/json" \
  -H "X-Client-Guid: $(uuidgen)" \
  -d '{
    "memberId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "clientGuid": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "performedByUserId": "9c8b7a65-4321-0fed-cba9-876543210fed"
  }'

# Registro de socio
curl -X POST https://localhost:5001/api/members \
  -H "Authorization: Bearer $JWT" \
  -H "Content-Type: application/json" \
  -d '{
    "fullName": "Ana García",
    "photoWebPBase64": "data:image/webp;base64,UklGRlYA...",
    "membershipEndDate": "2027-03-01"
  }'
```

---

## POST /api/members/{id}/freeze

Congela la membresía de un socio. Bloquea el acceso inmediatamente y extiende `MembershipEndDate`.

**Roles permitidos:** `Admin`, `Owner`

### Path Params

| Param | Tipo | Descripción |
|---|---|---|
| `id` | `Guid` | ID del socio a congelar |

### Request Body

```json
{
  "startDate": "2026-04-01",
  "endDate": "2026-04-14"
}
```

| Campo | Tipo | Validación |
|---|---|---|
| `startDate` | `DateOnly` | Requerido. No puede ser anterior a hoy |
| `endDate` | `DateOnly` | Requerido. Debe ser posterior a `startDate` |

**Duración calculada:** `endDate - startDate + 1` días (inclusive). Mínimo: **7 días**.

### Respuestas

#### 200 OK — Congelamiento aplicado

```json
{
  "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "memberId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "startDate": "2026-04-01",
  "endDate": "2026-04-14",
  "durationDays": 14,
  "createdByUserId": "9c8b7a65-4321-0fed-cba9-876543210fed",
  "createdAt": "2026-03-30T20:00:00Z"
}
```

> El `MembershipEndDate` del socio fue extendido en `durationDays` días de forma automática.

#### 400 Bad Request — Validación fallida

```json
{
  "title": "No se pudo congelar la membresía.",
  "detail": "La duración mínima de un congelamiento es 7 días. Duración calculada: 3 días.",
  "status": 400
}
```

Posibles causas:
- Duración menor a 7 días.
- El socio ya tiene 4 congelamientos en el año calendario (`startDate.Year`).
- El socio no tiene status `Active` (ya está `Frozen` o `Expired`).

#### 404 Not Found

```json
{
  "title": "Socio no encontrado.",
  "detail": "Socio con Id '3fa85f64-...' no encontrado.",
  "status": 404
}
```

#### 403 Forbidden

El rol del usuario autenticado no es `Admin` ni `Owner`.

---

## DELETE /api/members/{id}/freeze

Descongela la membresía de un socio. Restaura el status a `Active`.

> ⚠️ El `MembershipEndDate` extendido al congelar **no se revierte**. La extensión es permanente.

**Roles permitidos:** `Admin`, `Owner`

### Path Params

| Param | Tipo | Descripción |
|---|---|---|
| `id` | `Guid` | ID del socio a descongelar |

### Respuestas

#### 200 OK — Membresía activa nuevamente

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "fullName": "Juan Pérez",
  "photoWebPUrl": "/photos/3fa85f64-5717-4562-b3fc-2c963f66afa6.webp",
  "status": "Active",
  "membershipEndDate": "2027-01-14"
}
```

#### 400 Bad Request — Socio no está Frozen

```json
{
  "title": "No se pudo descongelar la membresía.",
  "detail": "El socio no está congelado. Estado actual: 'Active'.",
  "status": 400
}
```

#### 404 Not Found — Socio no encontrado

#### 403 Forbidden — Rol insuficiente

---

## GET /api/members/{id}/freezes

Retorna el historial completo de congelamientos de un socio, ordenado por `startDate` descendente.

**Roles permitidos:** `Admin`, `Owner`

### Path Params

| Param | Tipo | Descripción |
|---|---|---|
| `id` | `Guid` | ID del socio |

### Respuestas

#### 200 OK

```json
[
  {
    "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "memberId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "startDate": "2026-04-01",
    "endDate": "2026-04-14",
    "durationDays": 14,
    "createdByUserId": "9c8b7a65-4321-0fed-cba9-876543210fed",
    "createdAt": "2026-03-30T20:00:00Z"
  }
]
```

Array vacío `[]` si el socio no tiene congelamientos.

#### 404 Not Found — Socio no encontrado

---

## Curl de ejemplo — HU-07

```bash
# Congelar membresía
curl -X POST https://localhost:5001/api/members/3fa85f64-5717-4562-b3fc-2c963f66afa6/freeze \
  -H "Authorization: Bearer $JWT" \
  -H "Content-Type: application/json" \
  -d '{
    "startDate": "2026-04-01",
    "endDate": "2026-04-14"
  }'

# Descongelar membresía
curl -X DELETE https://localhost:5001/api/members/3fa85f64-5717-4562-b3fc-2c963f66afa6/freeze \
  -H "Authorization: Bearer $JWT"

# Historial de congelamientos
curl https://localhost:5001/api/members/3fa85f64-5717-4562-b3fc-2c963f66afa6/freezes \
  -H "Authorization: Bearer $JWT"
```
