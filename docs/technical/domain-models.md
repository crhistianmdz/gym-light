# Modelos de Dominio

Este documento contiene un mapeo de los modelos de dominio utilizados en el backend del proyecto GymFlow Lite.

## Entidades por módulo

### Membresía

#### Member
| Propiedad          | Tipo         | Descripción                              |
|--------------------|--------------|------------------------------------------|
| Id                | Guid         | PK                                       |
| FullName          | string       | Nombre completo                          |
| PhotoWebPUrl      | string       | URL de la foto en formato WebP           |
| Status            | MemberStatus | Estado (Active, Frozen, Expired, etc.)   |
| AutoRenewEnabled  | bool         | Indica si la renovación está habilitada  |
| MembershipEndDate | DateOnly     | Fecha de finalización de la membresía    |

#### MembershipFreeze
| Propiedad    | Tipo     | Descripción                               |
|--------------|----------|-------------------------------------------|
| Id           | Guid     | PK                                       |
| MemberId     | Guid     | FK a Member                              |
| StartDate    | DateOnly | Inicio del congelamiento                 |
| EndDate      | DateOnly | Fin del congelamiento                    |
| DurationDays | int      | Duración en días                         |

### Check-in

#### AccessLog
| Propiedad        | Tipo   | Descripción                              |
|------------------|--------|------------------------------------------|
| Id              | Guid   | PK                                       |
| MemberId        | Guid   | FK a Member                              |
| Timestamp       | DateTime | Fecha y hora del registro               |
| ClientGuid      | Guid   | UUID v4 para idempotencia                |

### Antropometría

#### BodyMeasurement
| Propiedad    | Tipo         | Descripción                              |
|--------------|--------------|------------------------------------------|
| Id           | Guid         | PK                                       |
| MemberId     | Guid         | FK a Member                              |
| RecordedAt   | DateTime     | Fecha de medición (UTC)                  |
| WeightKg     | decimal      | Peso en kilogramos                       |
| BodyFatPct   | decimal      | Porcentaje de grasa corporal             |

### Ventas

#### Sale
| Propiedad          | Tipo       | Descripción                              |
|--------------------|------------|------------------------------------------|
| Id                | Guid       | PK                                       |
| ClientGuid        | Guid       | UUID generado desde el cliente           |
| PerformedByUserId | Guid       | Usuario que realizó la venta             |
| Total             | decimal    | Suma total de la venta                   |

#### SaleLine
| Propiedad     | Tipo   | Descripción                               |
|---------------|--------|-------------------------------------------|
| Id            | Guid   | PK                                       |
| SaleId        | Guid   | FK a Sale                                |
| ProductId     | Guid   | FK a Product                             |
| Quantity      | int    | Cantidad vendida                         |
| UnitPrice     | decimal | Precio unitario                          |

### Pagos

#### Payment
| Propiedad    | Tipo          | Descripción                                                   |
|--------------|---------------|---------------------------------------------------------------|
| Id           | Guid          | PK                                                            |
| GymId        | Guid          | FK a Gym                                                     |
| MemberId     | Guid?         | FK a Member (nullable — pagos POS pueden no tener socio)      |
| ClientGuid   | Guid          | UUID generado desde el cliente, garantiza idempotencia       |
| Amount       | decimal       | Monto (> 0)                                                  |
| Category     | PaymentCategory | Categoría del pago                                           |
| Date         | DateTime      | Fecha del pago                                               |
| Description  | string?       | Descripción opcional                                         |
| CreatedAt    | DateTime      | Fecha de creación del registro                               |

### Rutinas

#### ExerciseCatalog
| Propiedad   | Tipo   | Descripción                                      |
|-------------|--------|--------------------------------------------------|
| Id          | Guid   | PK                                               |
| Name        | string | Nombre del ejercicio                             |
| MuscleGroup | string | Grupo muscular (ej. Pecho, Espalda, Piernas)     |
| Description | string | Descripción opcional                             |
| IsGlobal    | bool   | true = catálogo del gimnasio, false = personalizado |

#### Routine
| Propiedad       | Tipo   | Descripción                                      |
|-----------------|--------|--------------------------------------------------|
| Id              | Guid   | PK                                               |
| Name            | string | Nombre de la rutina                              |
| Description     | string | Descripción opcional                             |
| IsPublic        | bool   | true = pool del gimnasio, false = privada del Trainer |
| CreatedByUserId | Guid   | Usuario que creó la rutina (Trainer/Admin/Owner) |

#### RoutineExercise
| Propiedad         | Tipo   | Descripción                                  |
|-------------------|--------|----------------------------------------------|
| Id                | Guid   | PK                                           |
| RoutineId         | Guid   | FK a Routine                                 |
| ExerciseCatalogId | Guid?  | FK a ExerciseCatalog (null si es nombre libre) |
| CustomExerciseName| string?| Nombre libre si no hay ExerciseCatalogId     |
| Sets              | int    | Número de series                             |
| Reps              | int    | Número de repeticiones por serie             |
| Order             | int    | Posición dentro de la rutina                 |

#### RoutineAssignment
| Propiedad  | Tipo     | Descripción                              |
|------------|----------|------------------------------------------|
| Id         | Guid     | PK                                       |
| RoutineId  | Guid     | FK a Routine                             |
| MemberId   | Guid     | FK a Member                              |
| AssignedAt | DateTime | Fecha de asignación (UTC)                |
| AssignedBy | Guid     | UserId del Trainer/Admin/Owner           |

#### WorkoutLog
| Propiedad        | Tipo     | Descripción                                    |
|------------------|----------|------------------------------------------------|
| Id               | Guid     | PK                                             |
| ClientGuid       | Guid     | UUID v4 para idempotencia (UNIQUE)             |
| MemberId         | Guid     | FK a Member                                    |
| RoutineId        | Guid     | FK a Routine                                   |
| PerformedAt      | DateTime | Fecha y hora del entrenamiento (UTC)           |

#### WorkoutExerciseEntry
| Propiedad        | Tipo   | Descripción                                      |
|------------------|--------|--------------------------------------------------|
| Id               | Guid   | PK                                               |
| WorkoutLogId     | Guid   | FK a WorkoutLog                                  |
| RoutineExerciseId| Guid   | FK a RoutineExercise                             |
| Completed        | bool   | true = ejercicio completado en esta sesión       |

## Relaciones

```
Member 1──* MembershipFreeze
Member 1──* BodyMeasurement
Member 1──* AccessLog
Member 1──* RoutineAssignment
Member 1──* WorkoutLog
Routine 1──* RoutineExercise
Routine 1──* RoutineAssignment
RoutineAssignment 1──* WorkoutLog
WorkoutLog 1──* WorkoutExerciseEntry
RoutineExercise 1──* WorkoutExerciseEntry
ExerciseCatalog 1──* RoutineExercise (opcional)
Sale 1──* SaleLine
Product 1──* SaleLine
Gym 1──* Payment
Member 1──* Payment (opcional)
```

## Enums

### MemberStatus
- Active
- Frozen
- Expired

### SaleStatus
- Active
- Cancelled

### PaymentCategory
- Membership = 0
- POS = 1

## Value Objects

Actualmente no hay objetos de valor detectados explícitamente.