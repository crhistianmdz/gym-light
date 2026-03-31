# Esquema de Base de Datos

## PostgreSQL — Tablas

### Members
| Columna       | Tipo         | Constraints                      |
|---------------|--------------|----------------------------------|
| Id            | UUID         | PK                               |
| FullName      | VARCHAR(200) | NOT NULL                         |
| PhotoWebPUrl  | VARCHAR(500) | NOT NULL                         |
| Status        | VARCHAR(20)  | NOT NULL, CHECK (Active/Frozen/Expired) |
| MembershipEndDate | TIMESTAMPTZ | NOT NULL                        |
| CreatedAt     | TIMESTAMPTZ  | NOT NULL                         |
| UpdatedAt     | TIMESTAMPTZ  | NOT NULL                         |

Índices:
- PRIMARY KEY (Id)

### AccessLogs
| Columna         | Tipo        | Constraints              |
|-----------------|-------------|--------------------------|
| Id              | UUID        | PK                       |
| ClientGuid      | UUID        | UNIQUE NOT NULL          |
| Timestamp       | TIMESTAMPTZ | NOT NULL                 |
| PerformedByUserId | UUID      | NOT NULL                 |
| WasAllowed      | BOOLEAN     | NOT NULL                 |
| IsOffline       | BOOLEAN     | NOT NULL                 |
| DenialReason    | VARCHAR(300)| NULLABLE                 |

Índices:
- UNIQUE idx_accesslogs_client_guid (ClientGuid)

### MembershipFreezes
| Columna       | Tipo         | Constraints                      |
|---------------|--------------|----------------------------------|
| Id            | UUID         | PK                               |
| MemberId      | UUID         | FK                              |
| StartDate     | TIMESTAMPTZ  | NOT NULL                         |
| EndDate       | TIMESTAMPTZ  | NOT NULL                         |
| DurationDays  | INTEGER      | NOT NULL                         |
| CreatedByUserId | UUID       | NOT NULL                         |
| CreatedAt     | TIMESTAMPTZ  | NOT NULL                         |

Índices:
- PRIMARY KEY (Id)
- INDEX IX_MembershipFreezes_MemberId_StartDate (MemberId, StartDate)

### Payments
| Columna | Tipo | Constraints |
|---------|------|-------------|
| Id | UUID | PK |
| GymId | UUID | NOT NULL, FK → Gyms(Id) |
| MemberId | UUID | NULL, FK → Members(Id) |
| ClientGuid | UUID | NOT NULL, UNIQUE |
| Amount | DECIMAL(18,2) | NOT NULL, CHECK > 0 |
| Category | SMALLINT | NOT NULL (0=Membership, 1=POS) |
| Date | TIMESTAMP | NOT NULL |
| Description | VARCHAR(500) | NULL |
| CreatedAt | TIMESTAMP | NOT NULL, DEFAULT NOW() |

Índices:
- IX_Payments_ClientGuid — UNIQUE (para idempotencia)
- IX_Payments_GymId_Date — para queries de ingresos por período

### Otros
Para las demás tablas (`Products`, `BodyMeasurements`, etc.) se puede extrapolar el formato similar visto en el contexto.

---

## PostgreSQL — Relaciones FK

- `AccessLogs.MemberId` → `Members.Id` (ON DELETE RESTRICT)
- `MembershipFreezes.MemberId` → `Members.Id` (ON DELETE CASCADE)
- `BodyMeasurements.MemberId` → `Members.Id` (ON DELETE CASCADE)

---

## IndexedDB (Dexie.js) — Stores

### Store: users (v1)
Primary key: id

Índices:
- status
- membershipEndDate

### Store: sync_queue (v1)
Primary key: guid

Índices:
- type
- timestamp

### Store: metadata (v1)
Primary key: key

### Versiones posteriores

- **v2**: Agrega `products`, `sales`
- **v3**: Agrega `error_queue`
- **v4**: Agrega `measurements`
- **v5**: Agrega `exercise_catalog`, `routines`, `routine_assignments`, `workout_logs`

---

## Estrategia de Sincronización

| Dexie Store       | PostgreSQL Tabla        |
|-------------------|-------------------------|
| users             | Members                 |
| sync_queue        | (No aplica, control offline) |
| metadata          | (No aplica)            |
| products          | Products               |
| sales             | Sales                  |
| measurements      | BodyMeasurements       |

---

# Notas Finales
La configuración de esquemas PostgreSQL está inspirada en el código EF Core con traducción de tipos. Para Dexie.js, el archivo utiliza versiones consecutivas para evolucionar los stores. Esto asegura trazabilidad entre esquemas locales y backend.