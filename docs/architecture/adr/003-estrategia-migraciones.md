# ADR-003: Database Migration Strategy

**Status**: 🟢 Accepted
**Date**: 2026-06-09
**Deciders**: @gymflow-tech-lead

---

## Context

GymFlow Lite uses PostgreSQL on the backend with Entity Framework Core as the ORM. We need a migration strategy that:

- Is **reproducible** across dev, staging, and production.
- Supports **rollback** if a migration fails in production.
- Does **not require manual SQL** (we want to stay in the EF Core ecosystem).
- Works with our **Docker-based** development and CI/CD pipeline.
- Avoids the pitfalls we hit in early development (specifically: the use of `EnsureCreatedAsync`).

---

## Decision

We adopt the following migration strategy:

1. **ORM**: Entity Framework Core 8 with the **Npgsql** provider for PostgreSQL.
2. **Migration generation**: `dotnet ef migrations add <Name>` from inside the Docker SDK container (not on the host).
3. **Migration application in dev**: automatic at startup, via `MigrateAsync()` (only when `ASPNETCORE_ENVIRONMENT=Development`).
4. **Migration application in prod**: **CI/CD pipeline only**, never at app startup.
5. **Forbidden**: `EnsureCreatedAsync()` — was replaced by `MigrateAsync()` after a production incident in 2024.
6. **Archived SQL Server migrations**: kept in `_archived/` for historical reference, never applied.

### Environment-specific behavior

| Environment | How migrations run | When |
|-------------|--------------------|------|
| Development | Auto at startup (`MigrateAsync()`) | When `ASPNETCORE_ENVIRONMENT=Development` |
| Staging | CI/CD step before deploy | On `main` branch push |
| Production | CI/CD step before deploy | On release tag push |

### Generation command (from host, via Docker)

```bash
docker run --rm \
  --network docker_gymflow-network \
  -v "$(pwd)":/workspace -w /workspace \
  -e "ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=gymflow_dev;Username=gymflow;Password=gymflow123" \
  mcr.microsoft.com/dotnet/sdk:8.0 \
  bash -c "dotnet tool install --global dotnet-ef --version 8.0.* --verbosity quiet && \
           export PATH=\"\$PATH:/root/.dotnet/tools\" && \
           dotnet ef migrations add <MigrationName> \
             --project docker/backend/GymFlow.WebAPI.csproj \
             --output-dir ../../src/backend/Infrastructure/Persistence/Migrations"
```

---

## Options Considered

### Option 1: `EnsureCreatedAsync()` ❌

**Pros**:
- Zero config; creates the schema from the model on first run.
- Fast for prototypes.

**Cons**:
- **Does not track migrations** — every run is a "create or skip" with no history.
- **Cannot evolve the schema** — if you add a column, EnsureCreated does not add it; you have to drop the DB.
- **Incompatible with production** — you cannot do "add column X to existing table" safely.
- **This caused a production incident in 2024** where the schema drifted from the model; rejected.

### Option 2: `MigrateAsync()` in dev + CI/CD in prod ✅ (chosen)

**Pros**:
- Tracks every schema change as a versioned migration.
- Supports rollback (`dotnet ef migrations remove` or `dotnet ef database update <previous>`).
- Auto-apply in dev removes the "forgot to migrate" friction.
- CI/CD in prod prevents the "two app instances both running migrations at startup" race condition.

**Cons**:
- More setup than EnsureCreated.
- Migrations must be reviewed in PRs (good practice anyway).

### Option 3: `MigrateAsync()` at startup in all environments ❌

**Pros**:
- Simplest "it just works" behavior.

**Cons**:
- **Race condition**: if two app instances start simultaneously in prod, they may both try to apply the same migration and corrupt the schema.
- **Slow startup**: migration logic runs even on a healthy app that just restarted.
- **No human review**: a bad migration goes straight to prod with no checkpoint.

### Option 4: SQL-first migrations (raw .sql files)

**Pros**:
- Total control over the SQL.
- Works with any ORM or no ORM.

**Cons**:
- Drift risk: the EF model and the raw SQL can diverge.
- Double maintenance: you must update the model AND write the SQL.
- Loses EF Core's compile-time checks on the schema.

### Option 5: Run `dotnet ef` directly on the host

**Pros**:
- Faster (no Docker overhead).

**Cons**:
- The host is missing `aspnet-runtime-8.0`; `dotnet-ef` requires it.
- Inconsistent environments: "works on my machine" problems.
- Documented as broken on this host; rejected.

---

## Consequences

### Positive

- Schema changes are auditable (one migration = one PR).
- Production deployments are safe (no race conditions, no surprise migrations).
- Local dev is frictionless (auto-apply).
- Rollback is possible (downgrade to a previous migration).

### Negative

- Devs must remember to generate migrations when they change the model.
- The Docker command is long; documented in AGENTS.md and the migration script.
- Archived SQL Server migrations are confusing for new devs (mitigated by `_archived/README.md`).

### Neutral

- We commit to EF Core + Npgsql. Switching to a different ORM (e.g., Dapper) would require a new ADR.
- CI/CD must be configured to run `dotnet ef database update` before deploying the app container.

---

## References

- [EF Core Migrations Overview](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- [Npgsql Documentation](https://www.npgsql.org/efcore/index.html)
- Internal incident report: "Schema drift with EnsureCreatedAsync, 2024-Q3" (see `_archived/incident-reports/`)
- AGENTS.md section 5 (operational commands)
