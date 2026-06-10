# SCHEMA-XXX: [Nombre del cambio de schema]

**Status**: 🟡 Draft | 🟢 Applied
**DB**: PostgreSQL | SQL Server | ...
**Created**: YYYY-MM-DD

---

## 🎯 Motivación

¿Por qué necesitamos cambiar el schema?

- Feature nueva requiere nueva tabla/columna
- Refactor para mejorar normalización
- Performance: agregar índice
- Bug fix: constraint mal definido

---

## 📋 Cambios

### Tabla: `nombre_tabla`

#### Agregar columna

```sql
ALTER TABLE nombre_tabla
ADD COLUMN nueva_columna VARCHAR(100) NOT NULL DEFAULT 'valor_default';
```

#### Modificar columna

```sql
ALTER TABLE nombre_tabla
ALTER COLUMN columna_existente TYPE BIGINT;
```

#### Agregar índice

```sql
CREATE INDEX idx_nombre_tabla_columna
ON nombre_tabla (columna);
```

#### Agregar constraint

```sql
ALTER TABLE nombre_tabla
ADD CONSTRAINT fk_nombre_tabla_otra
FOREIGN KEY (otra_id) REFERENCES otra_tabla(id);
```

### Tabla nueva: `nombre_nueva`

```sql
CREATE TABLE nombre_nueva (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  campo1 VARCHAR(100) NOT NULL,
  campo2 INT NOT NULL DEFAULT 0,
  created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  updated_at TIMESTAMPTZ,
  deleted_at TIMESTAMPTZ
);

CREATE INDEX idx_nombre_nueva_campo1 ON nombre_nueva (campo1);
```

---

## 🔄 Migración

### EF Core Migration

```bash
dotnet ef migrations add NombreMigracion \
  --project docker/backend/GymFlow.WebAPI.csproj \
  --output-dir ../../src/backend/Infrastructure/Persistence/Migrations
```

### Aplicar en dev

Las migraciones se aplican automáticamente en startup con `ASPNETCORE_ENVIRONMENT=Development`.

### Aplicar en prod

CI/CD aplica migraciones antes del deploy.

---

## 📦 Affected Areas

- `src/backend/Domain/Entities/...` — entidades
- `src/backend/Infrastructure/Persistence/Configurations/...` — EF configs
- `src/backend/Application/...` — lógica de negocio
- `src/frontend/...` — si cambia la API
- `docs/technical/database-schema.md` — actualizar doc

---

## ⚠️ Breaking Changes

- [ ] Sí — Requiere migration de datos
- [ ] No — Aditivo o backwards compatible

### Plan de Migración de Datos (si breaking)

```sql
-- Paso 1: Backfill
UPDATE tabla SET nueva_columna = '...' WHERE condicion;

-- Paso 2: Validar
SELECT COUNT(*) FROM tabla WHERE nueva_columna IS NULL;

-- Paso 3: Aplicar constraint NOT NULL
ALTER TABLE tabla ALTER COLUMN nueva_columna SET NOT NULL;
```

---

## 🔗 Referencias

- HU-XXX que motiva el cambio
- ADR-XXX (si hay decisión arquitectónica)
- Migration file path
