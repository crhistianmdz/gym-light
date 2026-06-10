# ADR-001: Technology Stack

**Status**: 🟢 Accepted
**Date**: 2026-06-09
**Deciders**: @gymflow-tech-lead

---

## Context

GymFlow Lite is a SaaS management platform for small gyms (10-200 members). The product must:

- Run online with an **offline-first** fallback (gym staff often have poor connectivity).
- Support **multiple roles** (Owner, Admin, Receptionist, Trainer, Member) with RBAC.
- Be **maintainable by a small team** (1-3 devs) without specialized ops.
- Deploy on **affordable infrastructure** (single VPS or containerized cloud).
- Be delivered as a **PWA** installable on tablets/phones at the gym front desk.

We need to choose a stack that balances developer productivity, offline capability, type safety, and operational simplicity.

---

## Decision

We adopt the following stack:

| Layer | Technology |
|-------|------------|
| Backend | .NET 8 Web API — Clean Architecture |
| Frontend | React + Vite — PWA (Service Workers + Web Manifest) |
| Cloud DB | PostgreSQL via Entity Framework Core (Npgsql) |
| Local DB | IndexedDB via Dexie.js |
| Auth | JWT (in memory) + Refresh Tokens in HttpOnly Cookies |
| Cache | Redis (server session) |
| UI Kit | Material Design (MUI) |
| Infra | Docker (app, db, redis) |

---

## Options Considered

### Option 1: .NET 8 + React + PostgreSQL + Dexie ✅ (chosen)

**Pros**:
- Strong typing end-to-end (C# + TypeScript) reduces runtime errors.
- EF Core + Npgsql: mature, well-documented ORM for PostgreSQL.
- Clean Architecture enforces clear boundaries (Domain, Application, Infrastructure, WebAPI).
- React + Vite: fast dev cycle, mature ecosystem, PWA support out of the box.
- Dexie.js: best-in-class IndexedDB wrapper with TypeScript support.
- Redis: standard for session/cache, easy to operate.
- Docker: portable, reproducible environments.

**Cons**:
- Two languages (C# + TypeScript) requires devs to context-switch.
- .NET 8 + EF Core has a learning curve for devs coming from JS/Python.
- MUI is opinionated (good for speed, restrictive for custom designs).

### Option 2: Node.js + React + PostgreSQL + Dexie

**Pros**:
- Single language (TypeScript) end-to-end.
- Faster iteration on small CRUD features.

**Cons**:
- Less type safety on the backend (even with TypeScript, runtime errors are more common).
- NestJS + TypeORM is good but not as mature as EF Core for complex domains.
- Weaker support for Clean Architecture conventions in the Node ecosystem.

### Option 3: Python + Django + React + PostgreSQL + Dexie

**Pros**:
- Django admin gives us a free backoffice.
- Easy to find Python devs.

**Cons**:
- Django ORM is less powerful than EF Core for complex domain models.
- Python's async story is improving but still weaker than .NET for I/O-heavy APIs.
- No strong typing on the backend (mypy helps but is not the default).

### Option 4: Firebase / Supabase (BaaS)

**Pros**:
- Zero backend code; auth, DB, storage out of the box.
- Fast time-to-market.

**Cons**:
- Vendor lock-in.
- Limited support for complex business rules (our domain has many).
- Costs scale poorly beyond a small user base.
- Offline sync is rudimentary compared to Dexie.js + custom logic.

---

## Consequences

### Positive

- Clear separation of concerns via Clean Architecture.
- Type safety reduces bugs in production.
- PWA + offline-first works well on gym tablets with poor connectivity.
- Docker makes local dev and prod deployment consistent.

### Negative

- Two languages to maintain.
- New devs need to learn both stacks (mitigated by good onboarding doc).
- MUI customization is harder than Tailwind/Bootstrap for unique designs.

### Neutral

- We commit to Microsoft + React ecosystems (both stable, long-term supported).
- Redis is a runtime dependency; we need to operate it (or use a managed service).

---

## References

- RFC-001: Architecture Offline Sync (covers Dexie.js + ClientGuid details)
- [Clean Architecture — Robert C. Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Dexie.js documentation](https://dexie.org/)
- [Vite PWA plugin](https://vite-pwa-org.netlify.app/)
