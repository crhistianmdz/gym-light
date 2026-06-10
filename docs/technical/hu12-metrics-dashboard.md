# Documentación Técnica: HU-12 Dashboard de Métricas

## **Resumen**

La Historia de Usuario HU-12 "Dashboard de Métricas" implementa un panel de control
financiero y operativo para `Owner` y `Admin` del gimnasio. Permite visualizar dos
métricas clave del negocio:

1. **Ingresos mensuales** desglosados por categoría (`Membership` / `POS`) en un período configurable.
2. **Tasa de churn anual** con el conteo de socios activos, no renovados y el porcentaje de cancelación.

Adicionalmente, HU-12 introduce la **entidad `Payment`** como registro financiero de
origen autoritativo para los cálculos del dashboard. Los pagos pueden ser de dos
categorías (`Membership` o `POS`) y se registran de forma **idempotente** mediante
`ClientGuid` (mismo mecanismo que ya usa el resto del proyecto — HU-04, HU-06, HU-09, HU-11).

> **Cambio de alcance:** HU-12 es un cambio **aditivo puro** — no modifica ninguna
> entidad existente (Member, Sale, etc.). Solo agrega la entidad `Payment`, sus
> tablas/índices, los 3 use cases, los 2 controllers, los 2 DTOs y la UI `/dashboard`.

---

## **Reglas de Negocio**

| Regla | Descripción | Fuente |
|---|---|---|
| R1 | Solo `Owner` y `Admin` pueden acceder al dashboard. Otros roles son redirigidos. | User_Stories §HU-12.1 |
| R2 | El reporte de ingresos devuelve un desglose mensual con `membership`, `pos` y `total`. | User_Stories §HU-12.2 |
| R3 | El reporte de churn devuelve `totalMembers`, `activeMembers`, `notRenewed` y `churnRate`. | User_Stories §HU-12.3 (adaptado — ver discrepancias) |
| R4 | La fórmula de churn es `churnRate = (notRenewed / totalMembers) * 100`, con guard `totalMembers > 0`. | Adaptación de §HU-12.3 — ver discrepancias |
| R5 | `RegisterPayment` valida `amount > 0`. | User_Stories §HU-12.4 |
| R6 | `RegisterPayment` es **idempotente** por `ClientGuid`: si ya existe, devuelve el pago original sin reprocesar. | User_Stories §HU-12.4 |
| R7 | `POST /api/payments` acepta roles `Owner`, `Admin`, `Receptionist`. | User_Stories §HU-12.4 |
| R8 | `GET /api/admin/metrics/*` acepta solo roles `Admin`, `Owner`. | Derivado de §HU-12.1 |
| R9 | El año del reporte de churn debe estar entre 2020 y el año actual. | User_Stories §HU-12.3 |
| R10 | El rango de fechas del reporte de ingresos debe cumplir `from <= to`. | Derivado de la implementación |
| R11 | El dashboard funciona offline: si no hay conexión, usa datos del store `payments` de IndexedDB. | User_Stories §HU-12.5 |
| R12 | Se muestra un banner amarillo "Mostrando datos locales — sin conexión" cuando `isOffline=true`. | User_Stories §HU-12.5 |
| R13 | El chart de Ingresos es de barras agrupado: Membresías en azul (#1976d2), POS en naranja (#ff9800). | User_Stories §HU-12.6 |
| R14 | El Churn Rate cambia de color según el valor: success < 10% ≤ warning < 20% ≤ error. | User_Stories §HU-12.6 |

---

## **Discrepancias con la spec original del backlog**

La spec del backlog ([`User_Stories_GymFlow.md`](../tasks/User_Stories_GymFlow.md), HU-12)
fue ajustada durante la implementación. Estas son las divergencias intencionales
documentadas para que la spec y el código queden alineados:

| # | Spec original (backlog) | Implementación real | Razón / impacto |
|---|---|---|---|
| D1 | `GET /api/admin/metrics/income?year={year}` devuelve los 12 meses del año | `GET /api/admin/metrics/income?from={from}&to={to}` acepta rango libre de fechas | Más flexible: permite consultar cualquier período. La UI lo explota con dos `TextField type="date"`. |
| D2 | `IncomeReportDto` no especificado, pero implícito: `month` (1-12) | `IncomeReportDto { From, To, TotalIncome, ByMonth[]: { Year, Month, Membership, Pos, Total } }` | El DTO real es **agregado** (no single-month). El frontend lo desglosa con `BarChart` agrupado por mes. |
| D3 | `ChurnReportDto { year, activeMembers, churnedMembers, churnRate }` | `ChurnReportDto { year, totalMembers, activeMembers, notRenewed, churnRate }` | Se renombró `churnedMembers → notRenewed` (semánticamente equivalente) y se agregó `totalMembers` (la base del cálculo, requerida para la fórmula de D4). |
| D4 | `churnRate = (churnedMembers / (activeMembers + churnedMembers)) * 100` | `churnRate = (notRenewed / totalMembers) * 100` con `totalMembers > 0` | El denominador es `totalMembers` (no `activeMembers + notRenewed`) porque `Member` puede estar en estados distintos a `Active`/`Expired` (`Frozen`, `Cancelled`). Si se sumaran solo los dos, se sub-reportaría churn. |
| D5 | `RegisterPaymentRequest { memberId?, amount, category, date, description?, clientGuid }` | `RegisterPaymentRequest { memberId?, amount, category, clientGuid, notes?, saleId? }` | (1) `date` se omite del request — el servidor asigna `Timestamp = DateTime.UtcNow` automáticamente. (2) `description` se renombró a `notes`. (3) Se agregó `saleId?` opcional (FK a `Sale` cuando el pago viene de una venta POS — la UI no lo envía hoy, pero el modelo lo soporta). |
| D6 | "4 cards con `activeMembers`, `churnedMembers`, `churnRate`, color de alerta si churnRate > 20%" | 4 cards: `Total Socios`, `Activos`, `No Renovaron`, `Churn Rate` (con color de alerta >20%) | Se agregó la card de `Total Socios` (base del cálculo). El threshold de color es 3 niveles: success < 10% ≤ warning < 20% ≤ error. |

> **Acción recomendada:** actualizar [`User_Stories_GymFlow.md`](../tasks/User_Stories_GymFlow.md)
> para que las CAs 2, 3 y 4 de HU-12 reflejen el estado real. Esto se propone como
> work item, no se aplica automáticamente en este PR (es HU-12 mismo).

---

## **Arquitectura Backend**

```
Domain
  └── Entities/Payment.cs                  ← Entidad nueva (factory valida amount > 0)
  └── Enums/PaymentCategory.cs             ← Membership = 0, POS = 1

Application
  └── DTOs/Metrics/PaymentDto.cs           ← Response: pago registrado
  └── DTOs/Metrics/IncomeReportDto.cs      ← { From, To, TotalIncome, ByMonth[] }
  └── DTOs/Metrics/ChurnReportDto.cs       ← { Year, TotalMembers, ActiveMembers, NotRenewed, ChurnRate }
  └── UseCases/Admin/RegisterPaymentUseCase.cs    ← Idempotencia por ClientGuid
  └── UseCases/Admin/GetIncomeReportUseCase.cs    ← Agrupa por (año, mes) y pivota por categoría
  └── UseCases/Admin/GetChurnReportUseCase.cs     ← Calcula churnRate

Infrastructure
  └── Persistence/Repositories/PaymentRepository.cs   ← GetMonthlyIncomeAsync, GetChurnStatsAsync
  └── Persistence/GymFlowDbContext.cs                ← DbSet<Payment> Payments + configuración EF
  └── Persistence/Migrations/20260413220107_InitialCreate.cs   ← Tabla consolidada

WebAPI
  └── Controllers/PaymentsController.cs              ← POST /api/payments
  └── Controllers/Admin/MetricsController.cs         ← GET /api/admin/metrics/{income,churn}
```

### Flujo `RegisterPaymentUseCase`

```
1. Validar Amount > 0 (R5)
      ↓
2. Idempotencia: si ClientGuid ya existe → devolver el pago existente (R6)
      ↓
3. Si MemberId fue provisto, verificar que el socio existe
      ↓
4. Payment.Create(memberId, amount, category, actingUserId, clientGuid, notes?, saleId?)
      ↓
5. paymentRepo.AddAsync(payment)
      ↓
6. Retornar PaymentDto
```

### Flujo `GetIncomeReportUseCase`

```
1. Validar from <= to (R10)
      ↓
2. paymentRepo.GetMonthlyIncomeAsync(from, to)  → List<MonthlyAggregateRow>
      ↓
3. Agrupar por (Year, Month) y pivotar en MonthlyBreakdownDto:
      - Membership = SUM(amount WHERE category == Membership)
      - Pos        = SUM(amount WHERE category == POS)
      - Total      = Membership + Pos
      ↓
4. Ordenar por (Year, Month) ascendente
      ↓
5. totalIncome = SUM(ByMonth.Total)
      ↓
6. Retornar IncomeReportDto { From, To, TotalIncome, ByMonth[] }
```

### Flujo `GetChurnReportUseCase`

```
1. Validar 2020 <= year <= UtcNow.Year (R9)
      ↓
2. memberRepo.GetChurnStatsAsync(year) → (totalMembers, activeMembers, notRenewed)
      ↓
3. churnRate = totalMembers > 0 ? (notRenewed / totalMembers) * 100 : 0  (R4)
      ↓
4. Retornar ChurnReportDto { Year, TotalMembers, ActiveMembers, NotRenewed, ChurnRate }
```

---

## **Database**

### Tabla `Payments`

| Columna | Tipo | Nullable | Descripción |
|---|---|---|---|
| `Id` | `uuid` PK | NO | Identificador del pago |
| `MemberId` | `uuid` FK → `Members(Id)` | SÍ | `NULL` para pagos POS sin socio asociado |
| `Amount` | `numeric(18,2)` | NO | Monto del pago (validado > 0) |
| `Category` | `integer` (enum) | NO | `0=Membership`, `1=POS` |
| `Timestamp` | `timestamptz` | NO | Momento del pago (UTC, asignado por el servidor) |
| `CreatedByUserId` | `uuid` FK → `AppUsers(Id)` | NO | Usuario que registró el pago |
| `Notes` | `text` | SÍ | Nota libre (antes `description` en la spec) |
| `SaleId` | `uuid` | SÍ | FK opcional a `Sales(Id)` cuando el pago viene de una venta |

**Índices:**

| Índice | Columnas | Propósito |
|---|---|---|
| `IX_Payments_ClientGuid` | `ClientGuid` | Lookup rápido para idempotencia |
| `IX_Payments_CreatedByUserId` | `CreatedByUserId` | Reportes por recepcionista |
| `IX_Payments_Timestamp` | `Timestamp` | Filtros por rango de fecha (income report) |
| `IX_Payments_MemberId_Timestamp` | `MemberId`, `Timestamp` | Historial de pagos por socio |

**Foreign Keys:**

```
FK_Payments_Members_MemberId           → Members(Id)        ON DELETE SET NULL
FK_Payments_AppUsers_CreatedByUserId   → AppUsers(Id)       ON DELETE RESTRICT
```

> **Nota:** `MemberId` está configurado con `OnDelete SetNull` para no perder
> pagos históricos si se elimina un socio.

### Migración

La tabla `Payments` está incluida en la migración consolidada
`20260413220107_InitialCreate.cs`. La migración original
`_archived/2026_0331_2135_AddPaymentTable.cs` fue archivada cuando se consolidó
el historial (ver [ADR-003](../architecture/adr/003-estrategia-migraciones.md)).

```bash
# Aplicar localmente (si fuera necesario)
dotnet ef database update \
  --project src/backend/Infrastructure \
  --startup-project src/backend/WebAPI
```

---

## **API Reference**

### `POST /api/payments`
Registra un nuevo pago. **Idempotente** por `ClientGuid`.

- **Auth:** `Admin`, `Owner`, `Receptionist`
- **Request body:**
  ```typescript
  {
    memberId?: string;          // UUID, opcional (null para POS sin socio)
    amount: number;             // > 0
    category: 0 | 1;            // 0=Membership, 1=POS
    clientGuid: string;         // UUID v4, requerido para idempotencia
    notes?: string;             // opcional
    saleId?: string;            // opcional, UUID de Sale
  }
  ```
- **Responses:**
  - `201 Created` — Pago creado, retorna `PaymentDto` + header `Location: api/payments/{id}`
  - `200 OK` — `ClientGuid` ya existía, retorna el pago original (idempotencia)
  - `400 Bad Request` — `amount <= 0` o `memberId` no existe
  - `401/403` — Sin auth o rol insuficiente

### `GET /api/admin/metrics/income?from={from}&to={to}`
Retorna el desglose de ingresos en el rango de fechas.

- **Auth:** `Admin`, `Owner`
- **Query params:**
  - `from` — `YYYY-MM-DD`, default `currentYear-01-01` (en la UI)
  - `to` — `YYYY-MM-DD`, default `today` (en la UI)
- **Response (`200 OK`):**
  ```json
  {
    "from": "2026-01-01",
    "to": "2026-06-10",
    "totalIncome": 1234567.89,
    "byMonth": [
      { "year": 2026, "month": 1, "membership": 500000.00, "pos": 100000.00, "total": 600000.00 },
      { "year": 2026, "month": 2, "membership": 480000.00, "pos": 154567.89, "total": 634567.89 }
    ]
  }
  ```
- **Errores:**
  - `400 Bad Request` — `from > to` o fechas inválidas

### `GET /api/admin/metrics/churn?year={year}`
Retorna las estadísticas de churn para el año indicado.

- **Auth:** `Admin`, `Owner`
- **Query params:**
  - `year` — `integer`, rango válido `2020..currentYear` (R9)
- **Response (`200 OK`):**
  ```json
  {
    "year": 2026,
    "totalMembers": 150,
    "activeMembers": 120,
    "notRenewed": 25,
    "churnRate": 16.67
  }
  ```
- **Errores:**
  - `400 Bad Request` — Año fuera de rango

---

## **Frontend**

### Árbol de componentes

```
src/frontend/src/
├── pages/
│   └── DashboardPage.tsx                  ← Página principal, RBAC + formatters
├── components/
│   └── Dashboard/
│       ├── IncomeChart.tsx                ← BarChart (recharts) azul/naranja
│       └── ChurnStats.tsx                 ← 4 cards (MUI Grid) con color dinámico
├── services/
│   └── dashboardService.ts                ← getIncomeReport, getChurnReport, registerPayment
└── db/gymflow.db.ts                       ← Dexie store: 'payments' (offline cache)
```

### `DashboardPage` (responsabilidad)
1. **RBAC:** si `user.role` no es `Owner` ni `Admin` → `<Navigate to="/" replace />`.
2. Maneja estado local: `fromDate`, `toDate`, `year`, `loading`, `error`, `incomeReport`, `churnReport`.
3. `useEffect` carga ambos reportes al montar.
4. Botones "Consultar" para recargar manualmente.
5. Renderiza `<Alert severity="warning">` si `isOffline === true` (R12).

### `IncomeChart` (responsabilidad)
- Recibe `data: MonthlyBreakdown[]` y renderiza `<BarChart>` de `recharts`.
- Si `data.length === 0` → "No hay datos para el período seleccionado."
- Etiquetas: "Membresías" (azul `#1976d2`) y "POS" (naranja `#ff9800`).
- Eje X: `"Ene 26"`, `"Feb 26"`, ... (formato corto: mes abreviado + año en 2 dígitos).

### `ChurnStats` (responsabilidad)
- 4 cards (MUI `<Grid>` responsive: `xs=12, sm=6, md=3`):
  - **Total Socios** — `totalMembers` (text.primary)
  - **Activos** — `activeMembers` (success.main)
  - **No Renovaron** — `notRenewed` (warning.main)
  - **Churn Rate** — `churnRate.toFixed(1)%` con color dinámico:
    - `< 10%` → success.main (verde)
    - `10% ≤ x < 20%` → warning.main (naranja)
    - `≥ 20%` → error.main (rojo)
- Si `totalMembers === 0` → `<Alert severity="info">Sin datos suficientes para calcular el churn.</Alert>`

### `dashboardService` (responsabilidad)
- `getIncomeReport(from, to)`: intenta fetch → si falla, fallback a `db.payments` y marca `isOffline: true`.
- `getChurnReport(year)`: intenta fetch → si falla, fallback a `db.users` (filtra por `status === 'Active'` y `'Expired'`) y marca `isOffline: true`.
- `registerPayment(req, createdByUserId)`: POST al backend y luego `db.payments.put()` con `syncStatus: 'synced'`.

### Tipos (en `dashboardService.ts`)

```typescript
export interface MonthlyBreakdown {
  year: number;
  month: number;
  membership: number;
  pos: number;
  total: number;
}

export interface IncomeReport {
  from: string;
  to: string;
  totalIncome: number;
  byMonth: MonthlyBreakdown[];
  isOffline?: boolean;
}

export interface ChurnReport {
  year: number;
  totalMembers: number;
  activeMembers: number;
  notRenewed: number;
  churnRate: number;
  isOffline?: boolean;
}

export interface RegisterPaymentRequest {
  memberId?: string;
  amount: number;
  category: 0 | 1;
  clientGuid: string;
  notes?: string;
  saleId?: string;
}
```

### Estrategia Offline

1. **Network-first** vía `fetchWithAuth` (mismo cliente HTTP del proyecto).
2. **Fallback IndexedDB:** si la red falla, se computa el reporte localmente desde `db.payments` / `db.users`.
3. **Banner visible:** `<Alert severity="warning">` cuando el reporte tiene `isOffline: true`.
4. **No hay sincronización diferida** de los reportes: el dashboard siempre lee el último estado conocido.

---

## **Tests**

### Backend (xUnit)

- `src/backend/Tests/UseCases/Payments/GetIncomeReportUseCaseTests.cs` — cobertura del use case income.
- `src/backend/Tests/UseCases/Payments/GetChurnReportUseCaseTests.cs` — cobertura del use case churn (incluye validación de año, cálculo de churnRate, edge cases con `totalMembers = 0`).
- (No se encontró `RegisterPaymentUseCaseTests.cs` — ver §Trabajo futuro).

### Frontend (Vitest)

- `src/frontend/src/__tests__/DashboardPage.spec.tsx` — render del page con mocks de `dashboardService` y `IncomeChart`.
- `src/frontend/src/__tests__/dashboardService.spec.ts` — cobertura de `getIncomeReport` y `getChurnReport`, incluyendo manejo de errores y fallback a IndexedDB.

---

## **Decisiones de diseño**

1. **Rango libre en income en vez de `?year={year}`** — Más flexible, permite consultas
   de períodos arbitrarios (Q1, últimos 30 días, año fiscal custom, etc.).
2. **`TotalMembers` en el churn** — Hace explícito el denominador de la fórmula y
   permite validar visualmente que `churnRate` está bien calculado.
3. **Sin `date` en `RegisterPaymentRequest`** — El servidor es la única fuente de verdad
   del timestamp, evitando drift de relojes cliente.
4. **3 niveles de color en churn** en vez de 2 — Da más granularidad: éxito < 10% vs warning < 20% vs error.
5. **Fallback offline completo al dashboard** — Replicas de los cálculos en el cliente
   (filtrar `db.payments` por fecha y agrupar) para que el dashboard sea usable sin red.

---

## **Trabajo futuro (no incluido en este PR)**

1. **Tests para `RegisterPaymentUseCase`** — Faltan. Deberían cubrir: idempotencia
   por `ClientGuid`, validación `amount > 0`, validación de `MemberId` inexistente.
2. **Sincronización diferida de pagos** — Hoy `registerPayment` es siempre online.
   Si la red falla, se podría encolar en `sync_queue` como hacen otras entidades.
3. **Migración archivada `_archived/2026_0331_2135_AddPaymentTable.cs`** — Queda
   como referencia histórica. Considerar borrarla tras el deploy a prod.
4. **Actualizar `User_Stories_GymFlow.md` HU-12** — Incorporar las discrepancias D1-D6
   para que la spec y el código queden en sync (work item sugerido: HU-12.1 doc-fix).
5. **UI para filtrar income por `gymId` multi-tenant** — El backend ya filtra por
   `GymId` del usuario autenticado, pero la UI no lo expone (es single-tenant hoy).
6. **Exportar reportes a CSV** — Patrón ya implementado en HU-06 (audit). Aplicarlo
   al dashboard sería trabajo de 1 turno.

---

**Implementado en:** commit `47a07c5` — "HU12 feat: add income and churn reporting use cases and corresponding tests"
**Documentación alineada con:** backlog [`User_Stories_GymFlow.md`](../tasks/User_Stories_GymFlow.md) §HU-12
**Sesión de cierre (engram):** observation #120 (2026-03-31)
