# Migración: AddRoutinesAndWorkoutLogs

Esta migración introduce las tablas relacionadas con rutinas, ejercicios y registros de entrenamiento para la funcionalidad HU-11 de Rutinas Digitales.

## Tablas Nuevas

### `ExerciseCatalog`
```sql
CREATE TABLE "ExerciseCatalogs" (
    "Id" UUID PRIMARY KEY,
    "Name" VARCHAR(100) NOT NULL,
    "Description" VARCHAR(500),
    "MediaUrl" VARCHAR(500),
    "IsCustom" BOOLEAN NOT NULL,
    "CreatedByUserId" UUID NULL,
    "CreatedAt" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW()
);
-- FK CreatedByUserId → AppUsers("Id") (ON DELETE SET NULL)
```

### `Routines`
```sql
CREATE TABLE "Routines" (
    "Id" UUID PRIMARY KEY,
    "Name" VARCHAR(200) NOT NULL,
    "Description" VARCHAR(1000),
    "IsPublic" BOOLEAN NOT NULL,
    "CreatedByUserId" UUID NOT NULL,
    "CreatedAt" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW()
);
-- FK CreatedByUserId → AppUsers("Id") (ON DELETE RESTRICT)
```

### `RoutineExercises`
```sql
CREATE TABLE "RoutineExercises" (
    "Id" UUID PRIMARY KEY,
    "RoutineId" UUID NOT NULL,
    "ExerciseCatalogId" UUID NULL,
    "CustomName" VARCHAR(100),
    "Order" INTEGER NOT NULL,
    "Sets" INTEGER NOT NULL,
    "Reps" INTEGER NOT NULL,
    "Notes" VARCHAR(500)
);
-- CK: ExerciseCatalogId OR CustomName must be set
-- FK RoutineId → Routines("Id") ON DELETE CASCADE
-- FK ExerciseCatalogId → ExerciseCatalogs("Id")
```

### `RoutineAssignments`
```sql
CREATE TABLE "RoutineAssignments" (
    "Id" UUID PRIMARY KEY,
    "RoutineId" UUID NOT NULL,
    "MemberId" UUID NOT NULL,
    "AssignedByUserId" UUID NOT NULL,
    "AssignedAt" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE
);
-- FK RoutineId → Routines("Id")
-- FK MemberId → Members("Id") (ON DELETE RESTRICT)
-- FK AssignedByUserId → AppUsers("Id") (ON DELETE RESTRICT)
```

### `WorkoutLogs`
```sql
CREATE TABLE "WorkoutLogs" (
    "Id" UUID PRIMARY KEY,
    "AssignmentId" UUID NOT NULL,
    "MemberId" UUID NOT NULL,
    "SessionDate" DATE NOT NULL,
    "ClientGuid" UUID NOT NULL,
    "CreatedAt" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW()
);
-- FK AssignmentId → RoutineAssignments("Id")
-- FK MemberId → Members("Id") (ON DELETE RESTRICT)
-- UNIQUE(ClientGuid)
```

### `WorkoutExerciseEntries`
```sql
CREATE TABLE "WorkoutExerciseEntries" (
    "Id" UUID PRIMARY KEY,
    "WorkoutLogId" UUID NOT NULL,
    "RoutineExerciseId" UUID NOT NULL,
    "Completed" BOOLEAN NOT NULL DEFAULT FALSE,
    "CompletedAt" TIMESTAMP WITHOUT TIME ZONE,
    "Notes" VARCHAR(500)
);
-- FK WorkoutLogId → WorkoutLogs("Id")
-- FK RoutineExerciseId → RoutineExercises("Id")
```
