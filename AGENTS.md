# AGENTS.md — GymFlow Lite (Definitive, FlowDocs Standard)

> **Single source of truth** for AI agents working in this repository.
>
> **Layer separation**: Business rules live in the PRD; architectural decisions live in ADRs; proposals live in RFCs. This file contains only **operational rules** for agents and developers.

---

## 1. Project Status

**GymFlow Lite** is a SaaS management platform for small gyms (10-200 members). It runs online with an offline-first fallback on the frontend.

- **HU 01-11**: implemented (backend + frontend)
- **HU 12**: metrics dashboard (in implementation)
- **HU 13**: CI/CD (in implementation, lives at `docs/tasks/HU-001-HU-099/HU-013-cicd.md`)
- **HU 14+**: pending

Every implementation must follow the **SDD Flow** defined in section 2 of this file. No code may contradict the PRD (`docs/PRD_GymFlow_Lite.md`) or the approved RFC (`docs/RFC_001_Architecture_Offline_Sync.md`).

### Documentation architecture

| Document type | Where | Purpose |
|---------------|-------|---------|
| Business rules | `docs/PRD_GymFlow_Lite.md` | What the product does |
| Architectural decisions | `docs/architecture/adr/` | Why we chose this stack/approach |
| Large proposals | `docs/architecture/rfc/` (new) or `docs/RFC_*.md` (root) | What we propose to change |
| Work items (HUs) | `docs/tasks/HU-001-HU-099/HU-NNN-*.md` (new) or `docs/technical/huNN-*.md` (legacy) | Scoped tasks |
| Operational rules (this file) | `AGENTS.md` | How agents and devs should work |

---

## 2. SDD Flow (Spec-Driven Development)

The SDD workflow is a **developer responsibility**, not an architectural decision. Follow it on every meaningful change.

### 2.1 Documentation Lifecycle (FlowDocs)

Every HU, ADR, or major change follows this cycle:

```
1. Proposal   → What do we want to solve? Why?
2. Spec       → What specific requirements?
3. Design     → How do we implement it? (architecture, data model, APIs)
4. Tasks      → How do we split the implementation into steps?
5. Apply      → Execute the steps (code + tests)
6. Verify     → Does it meet the acceptance criteria?
7. Archive    → Close the HU, move to docs/archive/
```

For small HUs, steps 1-4 are condensed into the HU itself (use `template-hu-simple.md`).
For large or critical HUs, each step generates a separate artifact (use `template-hu-sdd.md`).

### 2.2 Technical Implementation Flow

For every implementation task:

```
1. Validate the task against PRD + User Stories
2. If there is a structural change → create or update the corresponding RFC/ADR
3. Define DTOs / API contracts first
4. Implement in order:
   - Batch A: Domain + DB
   - Batch B: Logic + API
   - Batch C: UI + Offline
5. Verify that no business rule was broken (PRD section 3)
```

### 2.3 Commit Convention

Use [Conventional Commits](https://www.conventionalcommits.org/):

```
feat: new feature
fix: bug fix
refactor: code change without functional change
docs: documentation only
test: add or modify tests
chore: maintenance tasks
perf: performance improvement
```

**Forbidden** to add `Co-Authored-By` or any AI attribution in commits.

---

## 3. Templates

> Templates live in `docs/templates/`. See [ADR-004](docs/architecture/adr/004-estructura-documentacion-flowdocs.md) for the folder structure rationale.

| Situation | Template name |
|-----------|---------------|
| New simple HU | `template-hu-simple` |
| New large or critical HU | `template-hu-sdd` |
| Architectural decision | `template-adr` |
| Large change proposal | `template-rfc` |
| New product or feature | `template-prd` |
| Bug | `template-bug-fix` |
| Refactor | `template-refactor` |
| New API endpoint | `template-api-endpoint` |
| DB schema change | `template-db-schema` |
| New dev onboarding | `template-onboarding` |
| Release notes | `template-changelog` |
| Technical debt | `template-tech-debt` |

All templates are in `docs/templates/<category>/<name>.md`. **Golden rule**: do not create documents from templates proactively; only when there is a real need.

---

## 4. Project Map

### Architectural decisions (ADRs)

- [ADR-001: Technology Stack](docs/architecture/adr/001-stack-tecnologico.md)
- [ADR-002: Authentication Strategy](docs/architecture/adr/002-estrategia-autenticacion.md)
- [ADR-003: Database Migration Strategy](docs/architecture/adr/003-estrategia-migraciones.md)
- [ADR-004: Documentation Structure (FlowDocs)](docs/architecture/adr/004-estructura-documentacion-flowdocs.md)
- [ADR-005: Naming Conventions](docs/architecture/adr/005-convenciones-naming.md)

### Main documentation

- [Product PRD](docs/PRD_GymFlow_Lite.md) — business rules and scope
- [RFC-001: Architecture Offline Sync](docs/RFC_001_Architecture_Offline_Sync.md) — approved architectural decisions
- [Consolidated User Stories](docs/tasks/User_Stories_GymFlow.md) — acceptance criteria per HU

### Technical documentation per HU (HU 01-11)

- [HU-01: Check-in](docs/technical/hu01-checkin.md)
- [HU-02: Member Registration](docs/technical/hu02-member-registration.md)
- [HU-03: Sales](docs/technical/hu03-sales.md)
- [HU-04: Sync](docs/technical/hu04-sync.md)
- [HU-05: Auth](docs/technical/hu05-auth.md)
- [HU-06: Audit](docs/technical/hu06-audit.md)
- [HU-07: Freeze](docs/technical/hu07-freeze.md)
- [HU-08: Cancellation](docs/technical/hu08-cancellation.md)
- [HU-09: Anthropometry](docs/technical/hu09-anthropometry.md)
- [HU-10: Progress Chart](docs/technical/hu10-progress-chart.md)
- [HU-11: Routines](docs/technical/hu11-routines.md)

### General technical documentation

- [Folder structure](docs/technical/folder-structure.md)
- [Domain models](docs/technical/domain-models.md)
- [Database schema](docs/technical/database-schema.md)
- [Implementation status per HU](docs/technical/implementation-status.md)
- [Implementation patterns](docs/technical/patterns.md)
- [API Reference](docs/technical/api-reference.md)
- [Frontend Guide](docs/technical/frontend-guide.md)
- [Architecture](docs/technical/architecture.md)

### HUs in implementation

- [HU-013: CI/CD](docs/tasks/HU-001-HU-099/HU-013-cicd.md)

### Available templates

Browse [`docs/templates/`](docs/templates/) or see [section 3](#3-templates).

---

## 5. Useful Commands

### Local development (the only 3 you need daily)

```bash
# Start DB + Redis
docker compose -f docker/docker-compose.yml up -d postgres redis

# Backend
cd src/backend/WebAPI && dotnet run

# Frontend
cd src/frontend && npm install && npm run dev
```

### Migrations

> Full context and the long Docker command are in [ADR-003](docs/architecture/adr/003-estrategia-migraciones.md). The two commands you actually need:

```bash
# Reset local DB (when migrations are messy)
docker compose -f docker/docker-compose.yml down -v

# Check migration history
docker compose -f docker/docker-compose.yml exec postgres psql -U gymflow -d gymflow_dev -c 'SELECT * FROM "__EFMigrationsHistory";'
```

### FlowDocs audit (in `docs/others/`)

```bash
cd docs/others && bash flowdoc-audit.sh    # Audit docs structure
cd docs/others && bash flowdoc-check.sh     # Smoke test
```

> ⚠️ **DO NOT run** `flowdoc-migration.sh` — it is destructive, overwrites project files.

### Check repo status

```bash
git status
git diff --stat
```

---

## 6. Agent Rules

- **Role**: Orchestrator. Do NOT program directly. Delegate code work to sub-agents.
- **Memory**: Every technical decision must be persisted in **Engram** (What/Why/Where/Learned format).
- **Validation**: Verify technical claims before stating them. If unsure, say "let me verify" and check code/docs first.
- **Anti-immediacy**: No shortcuts. Real learning takes time. Better incomplete and correct than fast and broken.
- **Conventions first**: Conventions > Code. If a rule is in this file or in an ADR, it is law.
- **Trust the ADRs**: When in doubt about a technical choice, read the relevant ADR. Do not reinvent the decision.
- **Trust the PRD**: When in doubt about a business rule, read the PRD. Do not guess.
- **Conventional commits**: `feat:`, `fix:`, `docs:`, `refactor:`, `test:`, `chore:`, `perf:`. Never add AI attribution.

---

**Version**: 1.3 (minimal: business rules in PRD, decisions in ADRs, only operational rules here)
**Last updated**: 2026-06-09
**Maintenance**: This file is updated only when operational rules change. Business rules go in the PRD. Decisions go in ADRs.
