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