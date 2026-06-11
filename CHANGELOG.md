# Changelog

> Formato basado en [Keep a Changelog](https://keepachangelog.com/).
> Este proyecto usa [Semantic Versioning](https://semver.org/).
>
> **v1.0.0 es el primer release oficial de GymFlow Lite** — marca el momento en que el proyecto
> tiene licencia explícita (AGPL v3), modelo de distribución definido (self-hosted), gobernanza
> documentada (RFC-002) y producto core completo (12/12 HUs implementadas).

---

## [Unreleased]

### En progreso (Fase C del roadmap — ver RFC-002)
- HU-014: Despliegue local (cierre del pendiente abandonado)
- HU-016: CLI de GymFlow (`install`, `upgrade`, `backup`, `doctor`)
- HU-017: Schema versioning + migraciones aditivas
- ADR-008: Sistema de módulos opt-in (arquitectura)

### Added
- **HU-015: Sistema de plugins opt-in** — arquitectura extensible con IPlugin interface, PluginLoader para descubrimiento en runtime, PluginRegistry en PostgreSQL para estado enabled/disabled, 3 plugins base (Anthropometry, Routines, Sales), admin API (`/api/plugins`) y UI en `/admin/plugins`

---

## [1.0.0] - 2026-06-10

### 🎉 Primer release oficial

GymFlow Lite v1.0.0 es el primer release que considera el proyecto "donable, instalable y mantenible por terceros". Marca el cierre del ciclo fundacional: producto core + licencia + gobernanza + estrategia de distribución.

### Added

#### Producto
- **HU-01 a HU-12: las 12 Historias de Usuario del backlog original están implementadas**
  - HU-01: Validación de Acceso Offline (check-in)
  - HU-02: Registro de Socio con Foto Obligatoria
  - HU-03: Venta de Producto / POS
  - HU-04: Sincronización Automática e Idempotente
  - HU-05: Autenticación Robusta y Sesión Offline
  - HU-06: Auditoría de Check-ins
  - HU-07: Congelamiento de Membresía
  - HU-08: Cancelación con Acceso Residual
  - HU-09: Perfil Antropométrico y Progreso
  - HU-10: Visualización de Evolución (Gráficas)
  - HU-11: Asignación de Rutinas Digitales
  - HU-12: Dashboard de Métricas para el Dueño (ingresos + churn)

#### Infraestructura
- **HU-13: CI/CD pipeline** (workflows `ci.yml`, `release-please.yml`, `security-review.yml`)
  - C# linter (`dotnet format`) en CI
  - Security review en PRs a `develop`, `staging`, `main`
  - Release Please con conventional commits
  - Branch protection strategy documentada en `.github/branch-protection.md`
- **Docker Compose para desarrollo local** (Postgres + Redis + Backend + Frontend)
- **EF Core 8 con Npgsql + migraciones formales** (reemplazo de `EnsureCreatedAsync`)

#### Documentación
- **5 ADRs fundacionales** (ADR-001 a ADR-005)
  - Stack tecnológico
  - Estrategia de autenticación
  - Estrategia de migraciones
  - Estructura de documentación (FlowDocs)
  - Convenciones de naming
- **2 ADRs estratégicos** (ADR-006, ADR-007)
  - Licencia AGPL v3
  - Modelo Self-Hosted vs SaaS
- **RFC-001**: Architecture Offline Sync (aprobado)
- **RFC-002**: Modelo de Negocio y Gobernanza (aprobado)
- **13 docs técnicas de HU** (HU-01 a HU-13)
- **8 docs técnicas generales** (architecture, api-reference, database-schema, domain-models, folder-structure, frontend-guide, implementation-status, patterns)
- **FlowDocs framework** (12 templates en español)
- **AGENTS.md v1.3** (reglas operacionales para devs y agentes)

#### Legal
- **`LICENSE` file con AGPL v3 completo** (670 líneas, texto estándar GNU)

### Changed

- **AGENTS.md**: refactorizado de 220 líneas (v1.3) para que referencie ADRs/PRD sin duplicar contenido.
- **Convención de documentación**: separación clara de capas (PRD/ADR/RFC/HU/templates).
- **Convenciones de commit**: `feat:`, `fix:`, `docs:`, `refactor:`, `test:`, `chore:`, `perf:` — sin atribución de IA.

### Deprecated

- **`docs/tasks/HU13-cicd.md`** (raíz) → renombrado a `docs/tasks/HU-001-HU-099/HU-013-cicd.md` (formato FlowDocs estándar).
- **`docs/User_Stories_GymFlow.md`** (raíz) → movido a `docs/tasks/User_Stories_GymFlow.md` (formato FlowDocs estándar).
- **`docs/technical/huNN-*.md`** (formato legacy) → conservado para HU-01 a HU-11; las nuevas HUs (HU-12+) usan formato FlowDocs.

### Fixed

- **HU-12 (Dashboard de Métricas)**: 6 discrepancias intencionales entre la spec del backlog y la implementación, documentadas en `docs/technical/hu12-metrics-dashboard.md` (sección "Discrepancias con la spec original", D1-D6). La implementación real es la fuente de verdad.
- **HU-12**: agregada doc técnica que faltaba (única HU del backlog sin doc), cerrando la simetría 12/12.

### Security

- **Autenticación JWT dual-token**: access token in-memory (15 min) + refresh token en HttpOnly cookie (7 días). Defense-in-depth contra XSS y CSRF.
- **Security review automática en PRs** con `claude-code-action@beta` (anthropics).

### Documentación estratégica nueva

- **ADR-006**: decisión documentada de AGPL v3 (vs MIT, Apache 2.0, GPL v3, BSL, dual).
- **ADR-007**: decisión documentada de self-hosted (vs SaaS multi-tenant, SaaS multi-DB, open-core, híbrido).
- **RFC-002**: "contrato social" del proyecto — incluye:
  - Visión y objetivos duales (probar tecnologías + producto profesional)
  - Modelo de monetización oportunista (donaciones + servicios + soporte)
  - Precios sugeridos para servicios profesionales (calibrados para Latam)
  - Roles, flujo de contribución, política de releases
  - Code of Conduct resumido
  - Roadmap en 3 horizontes temporales con criterios de éxito
  - 10 riesgos existenciales con mitigaciones
  - Plan de salida si el maintainer deja el proyecto

---

## [0.x.x] - Releases pre-1.0 (2026-03 a 2026-05)

Los releases pre-1.0 no tienen changelog formal. La historia está en los commits:

- **2026-03-26**: sesión fundacional con 266 observaciones — inicio del proyecto.
- **2026-03-31**: cierre de HU-03, HU-04, HU-07, HU-08 (commits `6a06bb8`, `e557dab`, `88ff753`, `d9ceffb`).
- **2026-04-13**: HU-06 Audit (commit `66b91d7`), Docker setup (commits `c1edf73`, `338c2e7`), CI/CD workflows (commit `fc54d98`).
- **2026-04-13**: Migración a EF Core formal (reemplazo de `EnsureCreatedAsync`).
- **2026-04-13**: Cierre de HU-12 Dashboard de Métricas (commit `47a07c5`).
- **2026-04-13**: HU-11 Routines + Frontend init (commit `433064b`).
- **2026-04-13**: HU-10 Progress Chart (commit `a15395e`).
- **2026-04-13**: HU-09 Anthropometry (commit `51fb9a2`).
- **2026-06-09**: Refactor FlowDocs (commit `d9fcaf2`) — nueva estructura de docs, 12 templates, 5 ADRs, AGENTS.md limpio.
- **2026-06-10**: HU-12 doc técnica + HU-13 doc técnica + C# linter en CI (commit `5952a6d` + `6505951`).
- **2026-06-10**: v1.0.0 release con Fase A completa (LICENSE + ADR-006 + ADR-007 + RFC-002) en commit `aa3b990`.

---

## Tipos de cambio (Keep a Changelog)

- **Added**: features nuevas.
- **Changed**: cambios en features existentes.
- **Deprecated**: features que se van a quitar (aviso con tiempo).
- **Removed**: features quitadas.
- **Fixed**: bugs arreglados.
- **Security**: vulnerabilidades parchadas.

Para detalles de cada release, ver los commits en git history y las docs técnicas de cada HU.

[Unreleased]: https://github.com/gymflow-lite/gymflow/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/gymflow-lite/gymflow/releases/tag/v1.0.0
