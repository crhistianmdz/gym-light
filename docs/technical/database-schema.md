# Esquema de Base de Datos

## PostgreSQL — Tablas

### Members
| Columna              | Tipo         | Constraints                      |
|----------------------|--------------|----------------------------------|
| Id                  | UUID         | PK                               |
| FullName            | VARCHAR(200) | NOT NULL                         |
| PhotoWebPUrl        | VARCHAR(500) | NOT NULL                         |
| Status              | VARCHAR(20)  | NOT NULL, CHECK (Active/Frozen/Expired) |
| MembershipEndDate   | TIMESTAMPTZ  | NOT NULL                         |
| CreatedAt           | TIMESTAMPTZ  | NOT NULL                         |
| UpdatedAt           | TIMESTAMPTZ  | NOT NULL                         |

Índices:
- PRIMARY KEY (Id)

### Products
| Columna   | Tipo          | Constraints                                  |
|-----------|---------------|----------------------------------------------|
| Id        | UUID          | PK                                           |
| Sku       | VARCHAR(100)  | UNIQUE, NOT NULL                             |
| Name      | VARCHAR(200)  | NOT NULL                                     |
| Stock     | INTEGER       | NOT NULL, CHECK (>= 0)                       |
| Price     | DECIMAL(18,2) | NOT NULL                                     |
| CreatedAt | TIMESTAMPTZ   | NOT NULL                                     |

Índices:
- UNIQUE idx_products_sku (Sku)

### Sales
| Columna      | Tipo         | Constraints                      |
|--------------|--------------|----------------------------------|
| Id           | UUID         | PK                               |
| ClientGuid   | UUID         | UNIQUE, NOT NULL                 |
| MemberId     | UUID         | FK → Members(Id)                 |
| Total        | DECIMAL(18,2)| NOT NULL                         |
| CreatedAt    | TIMESTAMPTZ  | NOT NULL                         |

Índices:
- UNIQUE idx_sales_client_guid (ClientGuid)

### SaleLines
| Columna    | Tipo         | Constraints                      |
|------------|--------------|----------------------------------|
| Id         | UUID         | PK                               |
| SaleId     | UUID         | FK → Sales(Id)                   |
| ProductId  | UUID         | FK → Products(Id)                |
| Quantity   | INTEGER      | NOT NULL                         |
| UnitPrice  | DECIMAL(18,2)| NOT NULL                         |

Relaciones:
- SaleLines.SaleId → Sales.Id (ON DELETE CASCADE)
- SaleLines.ProductId → Products.Id (ON DELETE RESTRICT)

### Payments
| Columna      | Tipo         | Constraints                      |
|--------------|--------------|----------------------------------|
| Id           | UUID         | PK                               |
| MemberId     | UUID         | FK → Members(Id)                 |
| ClientGuid   | UUID         | UNIQUE, NOT NULL                 |
| Amount       | DECIMAL(18,2)| NOT NULL, CHECK > 0              |
| Category     | SMALLINT     | NOT NULL (0=Membership, 1=POS)   |
| CreatedAt    | TIMESTAMPTZ  | NOT NULL                         |

Índices:
- UNIQUE idx_payments_client_guid (ClientGuid)
- INDEX idx_payments_member_creation (MemberId, CreatedAt)

### BodyMeasurements
| Columna       | Tipo          | Constraints                      |
|---------------|---------------|----------------------------------|
| Id            | UUID          | PK                               |
| MemberId      | UUID          | FK → Members(Id)                 |
| WeightKg      | DECIMAL(7,2)  | NOT NULL                         |
| BodyFatPct    | DECIMAL(5,2)  | NOT NULL                         |
| RecordedAt    | TIMESTAMPTZ   | NOT NULL                         |

Relaciones:
- BodyMeasurements.MemberId → Members.Id (ON DELETE CASCADE)

### WorkoutLogs
| Columna      | Tipo         | Constraints                      |
|--------------|--------------|----------------------------------|
| Id           | UUID         | PK                               |
| MemberId     | UUID         | FK → Members(Id)                 |
| ClientGuid   | UUID         | UNIQUE, NOT NULL                 |
| RecordedAt   | TIMESTAMPTZ  | NOT NULL                         |

Índices:
- UNIQUE idx_workoutlogs_client_guid (ClientGuid)
- INDEX idx_workoutlogs_member_recorded (MemberId, RecordedAt)

### Routines
| Columna         | Tipo         | Constraints                      |
|-----------------|--------------|----------------------------------|
| Id              | UUID         | PK                               |
| CreatedByUserId | UUID         | FK → AppUsers(Id)                |
| Name            | VARCHAR(100) | NOT NULL                         |
| CreatedAt       | TIMESTAMPTZ  | NOT NULL                         |

Relaciones:
- Routines.CreatedByUserId → AppUsers(Id) (ON DELETE RESTRICT)

### RoutineAssignments
| Columna         | Tipo         | Constraints                      |
|-----------------|--------------|----------------------------------|
| Id              | UUID         | PK                               |
| MemberId        | UUID         | FK → Members(Id)                 |
| RoutineId       | UUID         | FK → Routines(Id)                |
| AssignedByUserId| UUID         | FK → AppUsers(Id)                |
| AssignedAt      | TIMESTAMPTZ  | NOT NULL                         |

Relaciones:
- RoutineAssignments.MemberId → Members.Id (ON DELETE RESTRICT)
- RoutineAssignments.RoutineId → Routines.Id (ON DELETE CASCADE)

### ExerciseCatalog
| Columna        | Tipo         | Constraints                      |
|----------------|--------------|----------------------------------|
| Id             | UUID         | PK                               |
| CreatedByUserId| UUID         | FK → AppUsers(Id) (nullable)     |
| Name           | VARCHAR(100) | NOT NULL                         |
| CreatedAt      | TIMESTAMPTZ  | NOT NULL                         |

Relaciones:
- ExerciseCatalog.CreatedByUserId → AppUsers.Id (ON DELETE SET NULL)

---

## Relaciones FK

- `WorkoutLogs.MemberId` → `Members.Id` (ON DELETE RESTRICT)
- `RoutineAssignments.RoutineId` → `Routines.Id` (ON DELETE CASCADE)
- `RoutineAssignments.MemberId` → `Members.Id` (ON DELETE RESTRICT)
- `SaleLines.SaleId` → `Sales.Id` (ON DELETE CASCADE)

---

## Notas Finales
La documentación refleja el estado actual del schema basado en `Domain/Entities` y `GymFlowDbContext.cs`. Por cualquier modificación al esquema, generar migraciones EF Core según las reglas del equipo.