# ADR-004: Documentation Structure (FlowDocs Standard)

**Status**: 🟢 Accepted
**Date**: 2026-06-09
**Deciders**: @gymflow-tech-lead

---

## Context

GymFlow Lite is a small-to-medium project with 11+ HUs already implemented and more in the pipeline. We need a documentation structure that:

- Is **discoverable** (a new dev can find what they need in <2 minutes).
- Is **consistent** (every HU, ADR, RFC follows the same layout).
- **Scales** to 50+ HUs without becoming a mess.
- Supports **both legacy docs** (HU-01 to HU-11 already in `docs/technical/`) and **new docs** (HU-12+).
- Avoids **duplication** between operational rules (AGENTS.md) and business rules (PRD).
- Works in **Spanish** (the team's working language) without forcing a bilingual structure.

---

## Decision

We adopt the **FlowDocs** standard, adapted to our project as follows:

### Folder schema

```
docs/
├── PRD_GymFlow_Lite.md                          # Product PRD (business source of truth)
├── RFC_001_Architecture_Offline_Sync.md         # Approved RFCs at root
├── RFC_NNN-*.md                                 # New RFCs also at root
├── tasks/
│   ├── HU-001-HU-099/                           # Range folder for HUs 001-099
│   │   ├── HU-013-cicd.md                       # New HUs go here
│   │   └── HU-NNN-*.md
│   └── User_Stories_GymFlow.md                  # Consolidated user stories (historical)
├── technical/                                   # Technical documentation per HU
│   ├── hu01-checkin.md ... hu11-routines.md     # HU 01-11 (already documented, DO NOT move)
│   └── *.md                                     # Other technical docs
├── templates/                                   # Templates for new documents
│   ├── hu/
│   ├── adr/
│   ├── rfc/
│   ├── prd/
│   ├── bug-fix/
│   ├── refactor/
│   ├── api-endpoint/
│   ├── db-schema/
│   ├── onboarding/
│   ├── changelog/
│   └── tech-debt/
└── architecture/                                # New ADRs and RFCs
    ├── adr/                                     # Architecture Decision Records
    └── rfc/                                     # Requests for Comments
```

### Layer separation

| Document type | Lives in | Why |
|---------------|----------|-----|
| PRD (business rules) | `docs/PRD_*.md` | Source of truth for product behavior |
| ADRs (decisions) | `docs/architecture/adr/` | Audit trail of architectural choices |
| RFCs (large proposals) | `docs/architecture/rfc/` (new) or `docs/RFC_NNN-*.md` (root) | Proposals that need review |
| HUs (work items) | `docs/tasks/HU-001-HU-099/HU-NNN-*.md` (new) or `docs/technical/huNN-*.md` (legacy) | Scoped, time-bounded work |
| Tech docs (per HU) | `docs/technical/huNN-*.md` | Implementation details |
| Templates | `docs/templates/<category>/` | Reusable skeletons |

### What goes where — the rules

1. **PRD and root RFCs** always at `docs/` root.
2. **HU-01 to HU-11** stay in `docs/technical/`. Do NOT move them retroactively.
3. **New HUs (HU-12+)** go in `docs/tasks/HU-001-HU-099/HU-NNN-*.md`.
4. **ADRs and new RFCs** go in `docs/architecture/adr/` and `docs/architecture/rfc/`.
5. **Templates** are used on demand, not generated preemptively.

---

## Options Considered

### Option 1: No formal structure ❌

**Pros**:
- Zero setup.
- Maximum flexibility.

**Cons**:
- New devs cannot find anything.
- Inconsistent naming (`HU-1.md` vs `hu01-checkin.md` vs `checkin.md`).
- Will become a mess at 20+ HUs.
- Rejected; we have 11+ HUs already and the chaos is starting.

### Option 2: One folder per HU ❌

**Pros**:
- Maximum isolation.
- Easy to delete/archive.

**Cons**:
- 50+ folders for 50+ HUs is unmanageable.
- Cross-cutting concerns (e.g., shared types) have no home.
- Rejected.

### Option 3: `openspec/` (Spec-Driven Development) + `docs/`

**Pros**:
- Strong SDD methodology.
- Tooling exists (openspec CLI).

**Cons**:
- **Dual structure**: `openspec/` and `docs/` would overlap and confuse the team.
- The team already has an SDD workflow documented in AGENTS.md section 7.
- Adds a new tool/dependency without a clear win.
- Rejected.

### Option 4: FlowDocs (adapted) ✅ (chosen)

**Pros**:
- Mature, opinionated layout that covers all our document types.
- Templates enforce consistency.
- Supports both legacy and new docs (we don't have to rewrite HU-01 to HU-11).
- Spanish-only (no bilingual overhead).
- Templates are reusable across HUs/ADRs/RFCs.

**Cons**:
- Templates are biased toward greenfield projects; we adapt them (e.g., legacy HUs in `technical/` are not moved).
- "41 files" promise from the FlowDocs migration script is overkill; we generate only the templates we need.

### Option 5: GitBook / Notion / Confluence

**Pros**:
- Beautiful UI out of the box.
- Search built-in.

**Cons**:
- Vendor lock-in.
- Docs live outside the repo (no version control alongside code).
- Rejected; we want docs-as-code.

---

## Consequences

### Positive

- A new dev can navigate the docs in minutes.
- Naming is predictable (`HU-013-cicd.md` is always a HU, always numbered).
- Templates enforce quality (every HU has the same structure).
- Legacy docs (HU-01 to HU-11) are preserved without forced migration.

### Negative

- Two homes for HUs (`docs/technical/huNN-*.md` and `docs/tasks/HU-001-HU-099/HU-NNN-*.md`); mitigated by the rule "new HUs go to tasks/, legacy stays in technical/".
- The 12 templates are in Spanish; bilingual teams would need to duplicate them.
- Some FlowDocs anti-patterns (e.g., 41 empty files "to be ready") are explicitly rejected in our adaptation.

### Neutral

- The FlowDocs framework files themselves (`docs/others/flowdoc-*.sh`) are kept in the repo for the audit scripts, but their `migration.sh` is explicitly NOT used (it is destructive).
- The FlowDocs framework's own docs (in `docs/others/`) are ignored by git (`.gitignore` has `docs/others/*`); they are reference material, not project docs.

---

## References

- FlowDocs framework (in `docs/others/flowdoc-adoption-checklist.md`)
- ADR-005: Naming Conventions
- AGENTS.md section 6 (operational commands for docs)
