# Documentación Técnica: HU-017 Schema Versioning + Migraciones Aditivas

**Status**: ✅ Implemented
**HU**: HU-017 Schema Versioning + Migraciones Aditivas
**Created**: 2026-06-11

---

## Resumen

HU-017 establece el **sistema de versionado de schema** para GymFlow Lite. Cada instancia auto-hosted registra qué migraciones se aplicaron en la tabla `schema_version`, permitiendo upgrades seguros entre versiones, rollback automático y validación de políticas de migración aditiva.

Las migraciones siguen una **política estrictamente aditiva** (ADR-007): nunca se eliminan columnas ni se modifican destructivamente datos existentes. Esto garantiza compatibilidad hacia atrás y permite upgrades sin pérdida de datos.

---

## Reglas de Negocio — Política Aditiva

| Regla | Descripción | Acción requerida |
|---|---|---|
| R1 | **DropColumn**: bloqueado. No se puede eliminar una columna porque sus datos se pierden irreversiblemente. | Marcar la columna como obsoleta (`[Obsolete]`). Eliminarla en una versión mayor futura con migración explícita de datos. |
| R2 | **RenameColumn**: bloqueado. Rompe referencias semánticas sin migración de datos explícita. | Agregar nueva columna con el nombre deseado, migrar datos desde la anterior, marcar la anterior como obsoleta. |
| R3 | **AlterColumn incompatible**: bloqueado si (a) cambia el tipo de dato o (b) reduce `maxLength`. | Si se necesita un tipo diferente, crear una nueva columna y migrar datos. Si se necesita menos longitud, truncar solo con migración explícita aprobada. |
| R4 | **AlterColumn compatible**: permitido si solo cambia nullable (de `NOT NULL` a nullable) o aumenta `maxLength`. | Sin cambios requeridos. |
| R5 | **AddColumn, CreateTable, CreateIndex**: siempre permitidos. Son operaciones puramente aditivas. | Sin restricciones. |
| R6 | Agregar `maxLength` donde antes era `null`: bloqueado. Introduce una restricción de datos que puede causar truncamiento. | Migrar datos para verificar que cumplen la nueva longitud antes de aplicar. |

---

## Arquitectura

### Componentes del sistema de versionado

```
src/backend/
├── Domain/
│   ├── Entities/SchemaVersion.cs              ← Entidad: version (PK), module_name, applied_at, migration_hash, rollback_sql
│   └── Interfaces/
│       ├── ISchemaVersionRepository.cs         ← Repositorio: GetLatestVersion, GetByModule, RecordApplied, GetPendingBetween
│       ├── ISchemaMetadata.cs                  ← Metadata: disk_space, pg_version, advisory_lock helpers
│       ├── ISchemaLock.cs                      ← Lock: advisory lock PostgreSQL (ID 1701)
│       └── IMigrationExecutor.cs              ← Abstracción para ejecutar migraciones EF Core
├── Application/UseCases/Schema/
│   ├── UpgradeSchemaUseCase.cs                 ← Orquesta el upgrade completo con validación, backup, aplicación y rollback
│   ├── GetSchemaStatusUseCase.cs               ← Estado actual del schema: versión, pendientes, espacio en disco
│   └── ValidateSchemaUseCase.cs                ← Valida consistencia: migraciones huérfanas, violaciones de política
├── Infrastructure/
│   ├── Persistence/Migrations/                 ← Archivos de migración EF Core
│   │   ├── 20260413220107_InitialCreate.cs      ← Migración inicial (todas las tablas)
│   │   ├── 20250611120000_AddPluginRegistry.cs  ← Plugin Registry (HU-015)
│   │   └── 20260611123000_AddSchemaVersionTable.cs ← Tabla schema_version (HU-017)
│   └── Services/
│       ├── SchemaUpgrader.cs                    ← Algoritmo de upgrade: lock → pre-checks → backup → migraciones → record → unlock
│       ├── MigrationPolicy.cs                   ← Validador de política aditiva (análisis estático de archivos .cs)
│       ├── BackupHelper.cs                      ← pg_dump/pg_restore con rotación (keep last 5)
│       ├── SchemaLock.cs                        ← Wrapper de advisory lock PostgreSQL
│       └── EfCoreMigrationExecutor.cs           ← Delegado a IMigrator.MigrateAsync()
└── Tests/Schema/
    ├── SchemaUpgraderTests.cs                   ← Unit tests: happy path, lock, dry-run, rollback, semver
    ├── MigrationPolicyTests.cs                  ← Unit tests: operaciones bloqueadas, compatibles, directorio
    ├── IntegrationTests.cs                      ← Integration: seed 100 miembros, upgrade, integridad de datos
    ├── RollbackTests.cs                         ← Integration: fallo de migración, restauración con backup
    └── ConcurrentUpgradeTests.cs                ← Integration: concurrencia, lock exclusivo
```

### Algoritmo de upgrade

```
UpgradeSchema(targetVersion)
  │
  ├─ 1. Acquire advisory lock (ID 1701, exclusivo)
  │     └─ Si no se puede → error "otro upgrade en progreso"
  │
  ├─ 2. Pre-checks
  │     ├─ Validar que targetVersion es semver válido
  │     ├─ Obtener currentVersion desde schema_version
  │     ├─ Si currentVersion >= targetVersion → skip (ya aplicado)
  │     ├─ Obtener migraciones pendientes (currentVersion < migrationVersion <= targetVersion)
  │     └─ Validar espacio en disco (mínimo 500 MB libres)
  │
  ├─ 3. Backup (opcional, --skip-backup para desactivar)
  │     ├─ pg_dump --format=custom → backup_YYYYMMDD_HHMMSS.dump
  │     └─ Rotación: mantener últimos 5 backups
  │
  ├─ 4. Para cada migración pendiente (ordenadas por semver):
  │     ├─ Validar política aditiva (MigrationPolicy.ValidateFile)
  │     ├─ Ejecutar migración (EfCoreMigrationExecutor.ExecuteAsync)
  │     ├─ Si falla → ROLLBACK:
  │     │   ├─ pg_restore desde el backup creado en paso 3
  │     │   └─ Liberar lock → error con detalle
  │     └─ Si éxito → registrar en schema_version
  │
  └─ 5. Release advisory lock
       └─ Resultado: UpgradeResult { Success, AppliedMigrations, NewVersion, Duration }
```

### Tabla `schema_version`

```sql
CREATE TABLE schema_version (
    Version         VARCHAR(50)     NOT NULL PRIMARY KEY,  -- SemVer: "1.0.0"
    ModuleName      VARCHAR(200)    NOT NULL,              -- "core", "plugins", etc.
    AppliedAt       TIMESTAMPTZ     NOT NULL,              -- Fecha/hora de aplicación
    AppliedBy       VARCHAR(256)    NOT NULL,              -- Usuario o sistema que aplicó
    Description     VARCHAR(500),                          -- Descripción de la migración
    MigrationHash   VARCHAR(128)    NOT NULL,              -- SHA256 del archivo .cs
    RollbackSql     TEXT                                   -- SQL para deshacer la migración
);

CREATE INDEX IX_schema_version_module_name ON schema_version (ModuleName);
CREATE INDEX IX_schema_version_applied_at ON schema_version (AppliedAt);
```

---

## Linter de Política de Migración

### `scripts/check-migration-policy.py`

Herramienta de línea de comandos que escanea archivos de migración `.cs` buscando violaciones de la política aditiva. Debe ejecutarse en CI antes del build.

**Uso:**

```bash
python3 scripts/check-migration-policy.py src/backend/Infrastructure/Persistence/Migrations/
```

**Salida esperada:**

```
🔍 Scanning migration policy in: src/backend/Infrastructure/Persistence/Migrations/

  Files scanned: 3
  ✅ All migration files comply with the additive-only policy.
```

**En caso de violación (exit code 1):**

```
  ❌ Found 2 policy violation(s):

  src/.../Migrations/20250101000001_Bad.cs:42
  [DropColumn] Eliminar columnas causa pérdida irreversible de datos...
```

**Lo que valida:**

| Operación | Resultado |
|---|---|
| `migrationBuilder.DropColumn(...)` | ❌ SIEMPRE bloqueado |
| `migrationBuilder.RenameColumn(...)` | ❌ SIEMPRE bloqueado |
| `migrationBuilder.AlterColumn<T>(...)` con `oldClrType != T` | ❌ Cambio de tipo |
| `migrationBuilder.AlterColumn<T>(...)` con `maxLength < oldMaxLength` | ❌ Reducción de longitud |
| `migrationBuilder.AlterColumn<T>(...)` con `maxLength` y `oldMaxLength: null` | ❌ Nueva restricción |
| `migrationBuilder.AlterColumn<T>(...)` con `maxLength > oldMaxLength` | ✅ Permitido |
| `migrationBuilder.AlterColumn<T>(...)` con mismo tipo, solo nullable | ✅ Permitido |
| `migrationBuilder.AddColumn<T>(...)` | ✅ Permitido |
| `migrationBuilder.CreateTable(...)` | ✅ Permitido |
| `migrationBuilder.CreateIndex(...)` | ✅ Permitido |
| Archivos `.Designer.cs` | ⏭️ Ignorados |

---

## Integración CI

El linter se ejecuta en `.github/workflows/ci.yml` como paso obligatorio:

```yaml
- name: Lint migration policy
  run: python3 scripts/check-migration-policy.py src/backend/Infrastructure/Persistence/Migrations/
```

Si el linter encuentra violaciones, **el CI falla** y el PR no se puede mergear. Esto garantiza que ninguna migración destructiva llegue a producción.

---

## Auditoría de Migraciones Existentes

| Migración | Archivo | Operaciones | Cumple política |
|---|---|---|---|
| InitialCreate | `20260413220107_InitialCreate.cs` | `CreateTable` (×17), `CreateIndex` (×20) | ✅ 100% aditivo |
| AddPluginRegistry | `20250611120000_AddPluginRegistry.cs` | `CreateTable` (×1) | ✅ 100% aditivo |
| AddSchemaVersionTable | `20260611123000_AddSchemaVersionTable.cs` | `CreateTable` (×1), `CreateIndex` (×2) | ✅ 100% aditivo |

**Conclusión**: Las 3 migraciones existentes cumplen la política aditiva. No se encontraron DropColumn, RenameColumn ni AlterColumn incompatibles.

Los métodos `Down()` de todas las migraciones contienen `DropTable`, lo cual es **correcto y esperado**: el método `Down()` solo se ejecuta durante rollback manual y no afecta el camino de upgrade.

---

## Proceso de Upgrade

Ver la guía completa en [`docs/technical/upgrade-guide.md`](./upgrade-guide.md).

### Pasos resumidos

1. **Pre-upgrade**: hacer backup completo con `pg_dump`
2. **Validar política**: ejecutar `check-migration-policy.py`
3. **Ejecutar upgrade**: `gymflow-cli upgrade --target 1.1.0`
4. **Verificar**: `gymflow-cli status`
5. **Rollback** (si falla): `gymflow-cli rollback` o restaurar backup manualmente

---

## Backup y Rollback

### Backup automático

El `SchemaUpgrader` crea un backup automático antes de aplicar migraciones:

```bash
pg_dump --format=custom --file=backup_20260611_120000.dump gymflow_db
```

### Rotación

`BackupHelper` mantiene los últimos 5 backups (política de rotación). Los backups anteriores se eliminan automáticamente.

### Rollback automático

Si una migración falla durante el upgrade:
1. El `SchemaUpgrader` detecta el error
2. Restaura la base de datos desde el backup creado en el paso 3
3. Libera el advisory lock
4. Retorna el error detallado al caller

### Rollback manual

```bash
gymflow-cli rollback --target 1.0.0
# o manualmente:
pg_restore --clean --dbname=gymflow_db backup_20260611_120000.dump
```

---

## Validación de Schema

El comando `gymflow-cli doctor` incluye validación de schema:

```bash
gymflow-cli doctor
```

Verifica:
- ✅ Tabla `schema_version` existe
- ✅ Todas las migraciones en disco están registradas
- ✅ No hay migraciones huérfanas (registradas pero sin archivo)
- ✅ Las migraciones aplicadas cumplen la política aditiva
- ✅ Consistencia entre `schema_version` y `__EFMigrationsHistory`

---

## Referencias

- **ADR-007**: Modelo Self-Hosted (justifica migraciones aditivas)
- **ADR-003**: Estrategia de Migraciones (EF Core + PostgreSQL)
- **RFC-002**: Roadmap (schema versioning en el plan estratégico)
- **HU-017 Spec**: `docs/tasks/HU-001-HU-099/HU-017-schema-versioning.md`
- **Guía de Upgrade**: [`docs/technical/upgrade-guide.md`](./upgrade-guide.md)
- **Template DB Schema**: [`docs/templates/db-schema/template-db-schema.md`](../templates/db-schema/template-db-schema.md)
