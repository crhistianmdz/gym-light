# HU-11 — Rutinas Digitales

## Overview

**Purpose:** HU-11 agrega soporte para rutinas personalizadas y ejercicios. Incluye:
- Catálogo global de ejercicios que los `Trainer`, `Admin` y `Owner` pueden gestionar.
- Creación, edición y asignación de rutinas a miembros por `Trainer`, `Admin` y `Owner`.
- Historial de logs de entrenamiento registrado por `Member`, sincronizable offline.

**RBAC:**
- `Member`: Ve sus rutinas y registra logs.
- `Trainer`: Gestiona rutinas propias y ejercicios.
- `Admin`/`Owner`: Puede gestionar cualquier rutina o ejercicio.

---

## Domain Model

- `ExerciseCatalog`: Catálogo de ejercicios.
  - **Campos:** `id`, `name`, `description`, `muscleGroup`.
- `Routine`: Colección de ejercicios estructurados.
  - **Campos:** `id`, `name`, `isPublic` (bool), `createdByUserId`.
- `RoutineExercise`: Asociación entre una rutina y ejercicios.
  - **Campos:** `id`, `routineId`, `exerciseId`, `sets`, `reps`, `weight`.
- `RoutineAssignment`: Relación entre miembros y rutinas asignadas.
  - **Campos:** `id`, `routineId`, `assignedToMemberId`, `assignedByUserId`.
- `WorkoutLog`: Sesión de entrenamiento registrada.
  - **Campos:** `id`, `performedByMemberId`, `routineId`, `timestamp`.
- `WorkoutExerciseEntry`: Ejercicios realizados en un log.
  - **Campos:** `id`, `workoutLogId`, `exerciseId`, `sets`, `reps`, `weight`.

---

## API Endpoints

1. **`GET /api/exercises`**: Listar catálogo global.
   - **Query params:** `search`, `muscleGroup`, `page`, `pageSize`.
2. **`POST /api/exercises`**: Crear ejercicios (requiere rol).
3. **`GET /api/routines`**: Listar rutinas públicas o del creador autenticado.
4. **`POST /api/routines`**: Crear nueva rutina.
5. **`PUT /api/routines/{id}`**: Actualizar rutina existente.
6. **`GET /api/routines/member/{memberId}`**: Listar rutinas asignadas.
7. **`POST /api/routines/{id}/assign`**: Asignar rutina a un miembro.
8. **`POST /api/workout-logs`**: Registrar sesión de entrenamiento.
9. **`GET /api/workout-logs/member/{memberId}`**: Ver historial de logs de un miembro.

---

## Offline Strategy

1. **Estratégia Network-First:** Sincronización de rutinas y logs.
   - Datos almacenados en IndexedDB vía **Dexie.js** con fallback automático.
2. Stores locales en Dexie.js (versión 5):
   - `routines`
   - `routine_assignments`
   - `workout_logs`
   - `exercise_catalog`
   - Sync queue: Casos fallidos se reintentan cada 5 min o en reconexión.

---

## Dexie.js Schema (Versión 5)

```typescript
const db = new Dexie('gymflow')
db.version(5).stores({
  routines: 'id, name, isPublic',
  routine_assignments: 'id, routineId, assignedToMemberId',
  workout_logs: 'id, performedByMemberId, timestamp',
  exercise_catalog: 'id, name, muscleGroup',
})
```

---

## Frontend Components

### Árbol de componentes principales

- **`RoutinesPage`:** Página principal de gestión de rutinas.
  - **`MemberRoutineView`:** Vista de rutinas asignadas a un miembro.
  - **`RoutineBuilder`:** Constructor de nuevas rutinas.
  - **`ExerciseSelector`:** Selector de ejercicios para rutinas.
  - **`WorkoutLogPanel`:** Registro de logs de entrenamiento.
  - **`ExerciseRow`:** Componente reutilizable para filas de ejercicios.

---

## Business Rules

Decisiones clave tomadas:

1. Granularidad máxima con checkboxes por ejercicio.
2. Sin soporte para scheduling de rutinas (definido en Fase 3).
3. Catálogo global (visible para todos los entrenadores).
4. RBAC estricto (los roles restringen CRUD).
5. Plantillas de rutina públicas y privadas disponibles.
6. Sin purga automática de logs (la limpieza es manual).