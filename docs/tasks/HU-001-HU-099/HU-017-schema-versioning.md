# HU-017: Schema Versioning + Migraciones Aditivas

**Status**: ✅ Complete
**Owner**: @gymflow-tech-lead
**Created**: 2026-06-10
**Completed**: 2026-06-11
**Priority**: Must
**Estimación**: L

> **HU crítica para el modelo self-hosted.** Sin esta HU, el proyecto no puede cumplir la promesa de "si actualizo el proyecto no se borra la información del cliente". Esta HU es complementaria a [HU-015 Sistema de módulos](docs/tasks/HU-001-HU-099/HU-015-sistema-de-modulos.md).

---

## 🎯 Intent

Garantizar que un cliente pueda actualizar GymFlow Lite a una nueva versión **sin perder ni corromper su data**, mediante migraciones de DB siempre incrementales hacia adelante (nunca destructivas), versionado explícito del schema, y un proceso de upgrade atómico con backup pre-upgrade. Esto es el prerrequisito técnico del modelo self-hosted definido en [ADR-007](docs/architecture/adr/007-modelo-self-hosted.md) y la promesa explícita de [RFC-002](docs/architecture/rfc/002-modelo-de-negocio-y-gobernanza.md).

---

## 📋 Scope

### In Scope
- Versionado semántico del schema (`schema_version` table con version + timestamp + migración).
- Política explícita: **nunca DROP COLUMN, nunca cambiar tipo de manera incompatible**.
- Cada cambio de schema debe ser aditivo o tener un path de migración claro (multi-step).
- Script de upgrade (`gymflow upgrade`) que:
  1. Hace backup automático de la DB.
  2. Verifica que hay espacio en disco.
  3. Aplica migraciones pendientes en orden.
  4. Si falla, hace rollback automático.
- Migraciones por módulo (colabora con [HU-015](docs/tasks/HU-001-HU-099/HU-015-sistema-de-modulos.md)).
- Documentación de "Cómo upgrade entre versiones" para cada minor release.
- Tests de migración: simular N versiones atrás, aplicar upgrade, validar que la data está intacta.

### Out of Scope
- Hot-upgrade sin downtime (v2 — requeriría blue/green deployment).
- Migraciones de data (ej. encriptar columna existente con data) — para v1 solo cambios estructurales aditivos.
- Rollback a versiones más nuevas (downgrade) — no se soporta. Si algo falla, restaurar backup.

---

## 👥 User Story

**Como** cliente con GymFlow Lite instalado en producción
**Quiero** actualizar a una nueva versión sin perder mi data ni tener downtime significativo
**Para** recibir features nuevas y bugfixes con confianza

**Como** operador de GymFlow Lite (vos o un sysadmin)
**Quiero** que el proceso de upgrade sea 1 comando, atómico, y reversible
**Para** no romper nada en producción

**Como** developer que agrega una feature nueva
**Quiero** poder cambiar el schema de DB sin pensar "esto va a romper a los clientes que no actualizaron"
**Para** iterar rápido sin miedo

---

## ✅ Requirements

### MUST (obligatorios)
- [ ] Tabla `schema_version` registra cada migración aplicada (timestamp + versión + módulo).
- [ ] Política documentada: **nunca DROP COLUMN, nunca cambiar tipo incompatible** sin ruta de migración explícita.
- [ ] Script `gymflow upgrade` con backup pre-upgrade automático.
- [ ] El upgrade es atómico: si una migración falla, rollback completo, DB en estado anterior.
- [ ] Las migraciones se ejecutan en orden de versión (semver ascendente).
- [ ] Cada módulo ([HU-015](docs/tasks/HU-001-HU-099/HU-015-sistema-de-modulos.md)) tiene su propio set de migraciones en su carpeta.
- [ ] El orden de aplicación entre módulos respeta las dependencias declaradas.
- [ ] Documentación: "Cómo upgrade de v1.x a v1.y" con pasos para cada minor release.
- [ ] Tests: simular una DB con 5 versiones atrás, aplicar upgrade, validar data intacta.
- [ ] Logs estructurados del proceso de upgrade (qué migración, cuánto tardó, éxito/fallo).

### SHOULD (importantes)
- [ ] Comando `gymflow doctor` valida que la DB está en un estado consistente.
- [ ] Comando `gymflow status` muestra: versión actual del schema, última migración, espacio en disco, próximo upgrade sugerido.
- [ ] Pre-upgrade check: verificar que la versión de PostgreSQL es compatible.
- [ ] Pre-upgrade check: estimar tiempo de upgrade.
- [ ] Lock durante el upgrade: solo 1 proceso puede estar upgrade a la vez.

### COULD (deseables, futuro)
- [ ] Backup incremental (no full) para DBs grandes.
- [ ] Dry-run mode (`gymflow upgrade --dry-run`).
- [ ] Notificación post-upgrade (email al admin con resumen).
- [ ] Métricas de tiempo de upgrade (para SLA futuro).

---

## 🧪 Criterios de Aceptación (Given/When/Then)

- [ ] **Given** una instancia en v1.0 con datos de 100 socios, 50 ventas, 200 medidas
      **When** el operador corre `gymflow upgrade --target 1.1.0`
      **Then** el upgrade completa en <2 minutos, los 100 socios + 50 ventas + 200 medidas siguen ahí, el schema_version refleja v1.1.0

- [ ] **Given** el upgrade aplica una migración que falla a mitad de camino (ej. constraint violation)
      **When** falla
      **Then** el sistema hace rollback automático, la DB vuelve al estado pre-upgrade, el operador ve un error claro con qué migración falló y por qué

- [ ] **Given** un cliente tiene v1.0.5 y quiere ir directo a v1.2.0 (saltarse minors)
      **When** corre `gymflow upgrade --target 1.2.0`
      **Then** se aplican TODAS las migraciones intermedias (1.1.0, 1.1.1, 1.1.2, ..., 1.2.0) en orden, sin saltarse ninguna

- [ ] **Given** un dev quiere agregar una columna nueva a una tabla existente
      **When** crea la migración con `ADD COLUMN x VARCHAR(50) NULL` (aditiva)
      **Then** la migración se aplica sin tocar data existente, los clientes con DBs viejas pueden upgrade sin perder nada

- [ ] **Given** un dev quiere renombrar una columna (de "old_name" a "new_name")
      **When** lo intenta hacer con `RENAME COLUMN` directo
      **Then** el linter (o code review) lo bloquea, sugiere hacerlo en 2 migraciones: (1) `ADD COLUMN new_name` con backfill desde old_name, (2) `DROP COLUMN old_name` en la siguiente minor

- [ ] **Given** dos procesos intentan hacer upgrade simultáneamente
      **When** el segundo intenta arrancar
      **Then** falla con "otro upgrade en progreso, intentando más tarde", no se aplica doble

---

## 🔗 Dependencias

- **Depende de**:
  - [ADR-003 Migraciones](docs/architecture/adr/003-estrategia-migraciones.md) — base ya implementada con EF Core
  - PostgreSQL `pg_dump` para backups
  - [HU-015 Sistema de módulos](docs/tasks/HU-001-HU-099/HU-015-sistema-de-modulos.md) — las migraciones se organizan por módulo
- **Bloquea**:
  - La promesa de "upgrade sin perder data" del modelo self-hosted ([ADR-007](docs/architecture/adr/007-modelo-self-hosted.md))
  - [HU-016 CLI + installer](docs/tasks/HU-001-HU-099/HU-016-cli-installer.md) — el CLI es donde corre `gymflow upgrade`
- **Relacionado con**:
  - Semver (versions de la app = versions del schema)
  - Conventional commits (los `feat:` y `BREAKING CHANGE:` afectan el schema)

---

## 📦 Affected Areas

### Backend
- `src/backend/Infrastructure/Persistence/Migrations/` (existente, se reorganiza)
- `src/backend/GymFlow.Core/Schema/SchemaVersion.cs` (nueva, entidad)
- `src/backend/GymFlow.Core/Schema/SchemaUpgrader.cs` (nueva, lógica core)
- `src/backend/GymFlow.Core/Schema/MigrationPolicy.cs` (nueva, validaciones)
- `src/backend/Application/UseCases/Schema/UpgradeSchemaUseCase.cs` (nueva)
- `src/backend/Application/UseCases/Schema/GetSchemaStatusUseCase.cs` (nueva)
- `src/backend/Application/UseCases/Schema/ValidateSchemaUseCase.cs` (nueva)

### CLI (HU-016)
- `src/cli/Commands/UpgradeCommand.cs`
- `src/cli/Commands/StatusCommand.cs`
- `src/cli/Commands/DoctorCommand.cs`
- `src/cli/Helpers/BackupHelper.cs` (wrapper sobre `pg_dump`)

### DB
- Nueva tabla `schema_version` (migration adicional)
- Modificaciones a `__EFMigrationsHistory` (para que también registre versión semver)

### Docs
- `docs/technical/schema-versioning.md` (nueva, política completa)
- `docs/technical/upgrade-guide.md` (nueva, paso a paso entre versiones)
- `docs/templates/db-schema/template-db-schema.md` (actualizado con regla de aditividad)

### Tests
- `src/backend/Tests/Schema/SchemaUpgraderTests.cs`
- `src/backend/Tests/Schema/MigrationPolicyTests.cs`
- `src/backend/Tests/Schema/IntegrationTests.cs` (upgrade real en DB de test)
- `src/backend/Tests/Schema/RollbackTests.cs`

---

## 🧪 Verification

- [ ] Test: DB en v1.0 → upgrade a v1.1 → 100% de data preservada
- [ ] Test: DB en v1.0 → upgrade a v1.2.0 (saltando minors) → 100% de data preservada
- [ ] Test: migración que falla → rollback completo, DB intacta
- [ ] Test: dos upgrades simultáneos → solo uno se aplica, el otro espera
- [ ] Test: pre-upgrade check detecta espacio insuficiente
- [ ] Test: pre-upgrade check detecta versión de PostgreSQL incompatible
- [ ] Test: `gymflow doctor` valida DB consistente
- [ ] Test: `gymflow status` muestra info correcta
- [ ] Test: backup pre-upgrade existe después del upgrade
- [ ] Test: el upgrade tarda < 2 min para DB de 10K registros

---

## 📝 Notas

### Política de aditividad (la regla de oro)

**Toda migración DEBE ser aditiva o tener un plan de migración multi-step explícito.**

| Cambio | ¿OK en 1 migración? | Plan alternativo si no |
|---|---|---|
| `ADD COLUMN` nullable | ✅ Sí | — |
| `ADD COLUMN` con default | ✅ Sí | — |
| `CREATE TABLE` | ✅ Sí | — |
| `CREATE INDEX` | ✅ Sí | — |
| `ADD CONSTRAINT NOT NULL` a columna nullable | ⚠️ Depende | Backfillar en 2 pasos: (1) UPDATE todos los NULLs, (2) ADD CONSTRAINT |
| `DROP COLUMN` | ❌ NO | Deprecar primero (2 versiones minor), luego DROP en minor posterior |
| `RENAME COLUMN` | ❌ NO | (1) ADD COLUMN new_name, (2) UPDATE ... SET new_name = old_name, (3) DROP COLUMN old_name (2 minors después) |
| `ALTER COLUMN TYPE` (incompatible) | ❌ NO | (1) ADD COLUMN new_type, (2) backfill, (3) cambiar código que lee, (4) DROP old (varios releases) |
| `DROP TABLE` | ❌ NO | Deprecar + DROP en minor posterior |

**Razón**: en un modelo self-hosted con N versiones coexistiendo en el campo, una migración destructiva puede romper clientes que aún no actualizaron el código que espera la columna vieja.

### Estrategia de backup

```bash
# Pre-upgrade (gymflow upgrade hace esto automáticamente)
pg_dump -h $DB_HOST -U $DB_USER -d $DB_NAME \
    --no-owner --no-acl --clean --if-exists \
    --file="backups/gymflow-pre-v1.1.0-$(date +%Y%m%d-%H%M%S).sql"

# Si el upgrade falla:
psql -h $DB_HOST -U $DB_USER -d $DB_NAME < backups/gymflow-pre-v1.1.0-20260610-153045.sql
```

**Política de retención**: los últimos 5 backups se mantienen automáticamente. Backups más viejos se borran.

### Riesgos identificados

| Riesgo | Mitigación |
|---|---|
| El upgrade tarda mucho en DBs grandes | Pre-upgrade check estima tiempo; documentación dice "DBs >100K registros: preferir ventana de mantenimiento" |
| El backup no entra en el disco | Pre-upgrade check de espacio; ofrecer `--skip-backup` flag con warning |
| Migración destructiva accidental | Code review + linter que valida la política; ADR explícito |
| Bug en una migración | Rollback automático; el operador puede restaurar backup manual |
| DB de cliente tiene data corrupta pre-existente | `gymflow doctor` detecta y sugiere acción |

---

# Ciclo SDD (consolidado)

## 1. Proposal (resumido)

Garantizar upgrade seguro en modelo self-hosted. Sin esta HU, el proyecto no cumple la promesa fundamental de "no se borra la información del cliente". Es prerrequisito del [HU-016 CLI](docs/tasks/HU-001-HU-099/HU-016-cli-installer.md) (donde corre el comando `upgrade`).

## 2. Spec (resumida)

- Tabla `schema_version` con historial completo.
- Política aditiva enforced por code review + linter.
- Comando `gymflow upgrade` atómico con backup pre-upgrade.
- Comando `gymflow status` y `gymflow doctor` para diagnóstico.
- Migraciones por módulo (organización post [HU-015](docs/tasks/HU-001-HU-099/HU-015-sistema-de-modulos.md)).
- Lock para evitar upgrades concurrentes.

## 3. Design (resumido)

### Modelo de datos

```sql
CREATE TABLE schema_version (
    version VARCHAR(20) PRIMARY KEY,         -- semver, e.g., "1.1.0"
    module_name VARCHAR(100),                -- null = core, "sales" = módulo Sales
    applied_at TIMESTAMPTZ NOT NULL,
    applied_by VARCHAR(100),                  -- "gymflow-upgrade" o dev name
    description TEXT,
    migration_hash VARCHAR(64) NOT NULL,      -- sha256 del archivo de migración
    rollback_sql TEXT                        -- para rollback automático si falla
);

CREATE INDEX idx_schema_version_module ON schema_version(module_name);
CREATE INDEX idx_schema_version_applied_at ON schema_version(applied_at);
```

### Algoritmo de upgrade

```
function upgrade(targetVersion):
    1. acquire_lock('schema-upgrade')  # bloquea otros upgrades
    
    2. current = SELECT MAX(version) FROM schema_version WHERE module_name = '*core*'
    3. if current >= targetVersion:
         error("already at or beyond target version")
    
    4. pending = get_migrations_between(current, targetVersion)
    5. sort(pending, by version ASC)
    
    6. pre_check():
         - disk_space > estimated_backup_size * 2
         - PostgreSQL version compatible
         - no other processes writing to DB (advisory lock)
    
    7. backup_path = pg_dump(DB)
    8. save backup_path to schema_metadata
    
    9. for migration in pending:
         try:
             begin_transaction
             execute migration
             INSERT INTO schema_version
             commit_transaction
         except:
             rollback_transaction
             restore from backup
             error(f"migration {migration.version} failed: {exc}")
    
    10. release_lock('schema-upgrade')
    11. log_success(summary)
    12. return success
```

### Política de aditividad (linter)

Un script `scripts/check-migration-policy.py` valida cada `.cs` de migración contra las reglas:

```python
# Pseudo-código
for migration_file in migrations:
    content = read(migration_file)
    
    # Bloquea DROP COLUMN
    if "DropColumn" in content and "table" in content:
        warn("DROP COLUMN detected. Use deprecation + drop in next minor.")
    
    # Bloquea RENAME sin ruta
    if "RenameColumn" in content:
        warn("RENAME COLUMN detected. Use ADD + UPDATE + DROP in 2 minors.")
    
    # Bloquea ALTER TYPE incompatible
    if "AlterColumn" in content and "type:" in content:
        check_old_type_compatible_with_new(...)
    
    # OK: ADD COLUMN, CREATE TABLE, CREATE INDEX
```

Se ejecuta en CI (linter job). Falla el build si hay migración que viola la política.

## 4. Tasks (desglose)

### Batch A — Infraestructura de versioning (1 sprint)

1. **A1** — Crear entidad `SchemaVersion` y migración inicial (1 día)
2. **A2** — Crear `SchemaUpgrader` con algoritmo completo (3-4 días)
3. **A3** — Crear `MigrationPolicy` y linter (2-3 días)
4. **A4** — Implementar backup helper con `pg_dump` (1-2 días)
5. **A5** — Implementar `gymflow status` y `gymflow doctor` (en CLI, HU-016) (2 días)
6. **A6** — Tests unitarios del upgrader (2 días)
7. **A7** — Tests de integración con DB de test (2-3 días)
8. **A8** — Tests de rollback (1-2 días)
9. **A9** — Linter en CI (HU-13 ci.yml, agregar step) (medio día)
10. **A10** — Documentación `schema-versioning.md` (1 día)

**Criterio de fin Batch A**: el sistema de versioning existe, funciona, tiene tests pasando, linter integrado en CI.

### Batch B — Migración de las migraciones existentes (1 sprint)

1. **B1** — Auditar las migraciones existentes en `Migrations/`
2. **B2** — Aplicar la política: identificar cuáles violan (probablemente `DropColumn` de las migraciones archivadas)
3. **B3** — Para cada violación, decidir: ¿es histórico (no importa) o actual (hay que arreglar)?
4. **B4** — Documentar el estado de cada migración histórica
5. **B5** — Asegurar que `__EFMigrationsHistory` registra la versión semver correspondiente

**Criterio de fin Batch B**: las migraciones futuras siguen la política; las históricas están auditadas.

### Batch C — Upgrade guide y runbooks (paralelo)

1. **C1** — Crear `docs/technical/upgrade-guide.md` template
2. **C2** — Documentar upgrade de v1.0 → v1.1 (cuando exista v1.1)
3. **C3** — Crear runbook de "qué hacer si el upgrade falla"

**Criterio de fin Batch C**: hay guía para cada upgrade.

---

## 🔗 Referencias

- [ADR-003 Migraciones](docs/architecture/adr/003-estrategia-migraciones.md) — base ya implementada
- [ADR-007 Self-Hosted](docs/architecture/adr/007-modelo-self-hosted.md) — modelo de distribución que esta HU habilita
- [RFC-002 §6](docs/architecture/rfc/002-modelo-de-negocio-y-gobernanza.md#6-roadmap-estratégico) — el lugar en el roadmap
- [HU-015 Sistema de módulos](docs/tasks/HU-001-HU-099/HU-015-sistema-de-modulos.md) — feature paralela, las migraciones se organizan por módulo
- [HU-016 CLI + installer](docs/tasks/HU-001-HU-099/HU-016-cli-installer.md) — donde corre `gymflow upgrade`
- [HU-013 CI/CD](docs/technical/hu13-cicd.md) — el linter de política se integra acá
- Odoo upgrade strategy — precedente
- [template-hu-sdd.md](docs/templates/hu/template-hu-sdd.md) — template usado
