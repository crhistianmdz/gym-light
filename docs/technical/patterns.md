# Patrones de Implementación — GymFlow Lite

Este documento es la guía de referencia para implementar nuevas HUs.
Seguí estos patrones SIEMPRE — no improvises estructura.

---

## 1. Use Case (Backend)

```csharp
public class CreateRoutineUseCase(GymFlowDbContext db)
{
    public async Task<ResultType> ExecuteAsync(RequestType request, CancellationToken ct)
    {
        // Implementación lógica
    }
}
```

Reglas:
- Constructor recibe `GymFlowDbContext` por inyección
- Método principal: `ExecuteAsync(..., CancellationToken ct)`
- Retorna `Result<T>` o lanza excepción de dominio tipada

---

## 2. Controller (Backend)

```csharp
[ApiController]
[Route("api")]
public class RoutinesController(CreateRoutineUseCase createRoutine) : ControllerBase
{
    [HttpPost("routines")]
    public async Task<IActionResult> CreateRoutine([FromBody] CreateRoutineRequest request)
    {
        var result = await createRoutine.ExecuteAsync(request, default);
        return CreatedAtAction(nameof(GetRoutines), new { id = result.Id }, result);
    }
}
```

Reglas:
- `[ApiController]` + `[Route("api/[controller]")]`
- Inyecta Use Cases en constructor
- Retorna `IActionResult` con códigos HTTP explícitos

---

## 3. Registro en DI — Program.cs

```csharp
builder.Services.AddScoped<CreateRoutineUseCase>();
```

---

## 4. Agregar store en Dexie (Frontend)

```typescript
this.version(5).stores({
  routines: 'id, createdByUserId, isPublic, updatedAt'
});
```

Reglas:
- NUNCA modificar versiones anteriores
- Siempre incrementar número de versión
- Declarar la interface del nuevo store antes de la clase

---

## 5. Registrar evento en syncService (Frontend)

```typescript
const ENDPOINT_MAP: Record<SyncEventType, { url: (payload: Record<string, any>) => string; method: string }> = {
  WorkoutLogCreate: { url: () => '/api/workout-logs', method: 'POST' }
};

async updateLocalCache(type: SyncEventType, data: Record<string, unknown>): Promise<void> {
  if (type === 'WorkoutLogCreate') {
    const log = data as { clientGuid: string; id: string };
    await db.workout_logs.where('clientGuid').equals(log.clientGuid).modify({
      syncStatus: 'synced',
      id: log.id,
    });
  }
}
```

Reglas:
- Agregar el type en SyncEventType
- Agregar entrada en ENDPOINT_MAP
- Agregar caso en updateLocalCache

---

## 6. Service del frontend (Network-First)

```typescript
export async function getRoutines(): Promise<Routine[]> {
  try {
    const res = await fetch('/api/routines');
    if (!res.ok) throw new Error('API error');
    const data: Routine[] = await res.json();
    await db.table('routines').bulkPut(data);
    return data;
  } catch {
    return db.table('routines').toArray();
  }
}
```

---

## 7. Página React

```typescript
export function RoutinesPage() {
  const [routines, setRoutines] = useState<Routine[]>([]);

  useEffect(() => {
    getRoutines().then(setRoutines);
  }, []);

  return (
    <Container>
      {routines.map(routine => (
        <Card key={routine.id}>
          <CardHeader title={routine.name} />
        </Card>
      ))}
    </Container>
  );
}
```

Reglas:
- Funcional, TypeScript estricto, sin `any`
- Imports absolutos con alias `@/`

---

## 8. Patrón Repository (Backend)

### Interface (Domain layer):
```csharp
// Domain/Interfaces/IPaymentRepository.cs
public interface IPaymentRepository
{
    Task<Payment?> GetByClientGuidAsync(Guid clientGuid);
    Task<bool> ClientGuidExistsAsync(Guid clientGuid);
    Task AddAsync(Payment payment);
    Task<IEnumerable<MonthlyAggregateRow>> GetMonthlyIncomeAsync(Guid gymId, int year);
}
```

### Implementación (Infrastructure layer):
```csharp
// Infrastructure/Persistence/Repositories/PaymentRepository.cs
public class PaymentRepository : IPaymentRepository
{
    private readonly GymFlowDbContext _db;
    public PaymentRepository(GymFlowDbContext db) => _db = db;

    public Task<bool> ClientGuidExistsAsync(Guid clientGuid) =>
        _db.Payments.AnyAsync(p => p.ClientGuid == clientGuid);

    public Task<Payment?> GetByClientGuidAsync(Guid clientGuid) =>
        _db.Payments.FirstOrDefaultAsync(p => p.ClientGuid == clientGuid);

    public async Task AddAsync(Payment payment)
    {
        await _db.Payments.AddAsync(payment);
        await _db.SaveChangesAsync();
    }
    // ...
}
```

### Inyección en UseCase:
```csharp
// Application/UseCases/Admin/RegisterPaymentUseCase.cs
public class RegisterPaymentUseCase
{
    private readonly IPaymentRepository _payments;
    public RegisterPaymentUseCase(IPaymentRepository payments) => _payments = payments;

    public async Task<Result<PaymentDto>> ExecuteAsync(RegisterPaymentRequest req)
    {
        if (await _payments.ClientGuidExistsAsync(req.ClientGuid))
        {
            var existing = await _payments.GetByClientGuidAsync(req.ClientGuid);
            return Result.Ok(existing!.ToDto());
        }
        // ... crear y guardar
    }
}
```

### Registro en DI (Program.cs):
```csharp
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<RegisterPaymentUseCase>();
```

---

## 9. Convenciones de Código

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

## 10. Persistencia Local — Reglas

- **Prohibido `localStorage`** para datos de socios o transacciones. Solo IndexedDB.
- Solicitar `navigator.storage.persist()` al inicializar la app (HU-05).
- Limpiar `sync_queue` únicamente tras confirmación exitosa del servidor.

---

## 11. Offline / Red — Reglas

Estrategia Network-First obligatoria:

```
Request API
  ├─ Online → fetch() → actualizar store local → responder
  └─ Offline / timeout >2s → consultar IndexedDB → encolar en sync_queue → responder
```

- Retry automático cada **5 minutos** o al evento `online`.
- Si un registro falla **3 veces** → moverlo a "Bandeja de Errores" para revisión manual.
- Header `X-Data-Version` en cada respuesta: si es mayor a `metadata.dataVersion`, limpiar IndexedDB y re-descargar snapshot.
- Todo código I/O (red o IndexedDB) debe tener `try/catch` con fallback offline explícito.
- Prohibido silenciar errores de red con `catch (e) {}`.
- El indicador de sincronización en la UI debe reflejar: Verde (sincronizado) / Naranja (pendientes) / Gris (offline).