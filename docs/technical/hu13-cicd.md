# Documentación Técnica: HU-13 CI/CD

## **Resumen**

HU-13 establece la **infraestructura de Integración Continua y Despliegue Continuo**
del proyecto GymFlow Lite, basada en **GitHub Actions** sobre tres ramas
(`develop`, `staging`, `main`) que modelan los entornos dev/staging/producción.

El flujo garantiza que **ningún código roto o sin revisar llegue a producción**,
mediante:

1. **Pipeline CI** obligatorio en cada PR (lint + build + test).
2. **Revisión de seguridad** automática con `claude-code-action@beta` en PRs.
3. **Estrategia de protección de ramas** documentada (aplicación manual en GitHub Settings).
4. **Release Please** para versionado y release notes automáticos en cada merge a `main`.

> **Implementado en:** commit `fc54d98` — "feat: Add CI/CD workflows and branch protection strategy documentation"

---

## **Reglas de Negocio**

| Regla | Descripción | Fuente |
|---|---|---|
| R1 | El orquestador CI/CD es **GitHub Actions** (3 workflows: `ci.yml`, `release-please.yml`, `security-review.yml`). | Spec §2 |
| R2 | Toda funcionalidad entra vía **Pull Request**; prohibido push directo a `main`. | Spec §2, branch-protection.md |
| R3 | El pipeline CI corre en **PRs a `develop`, `staging` y `main`**, y en **push a `develop` y `staging`**. | ci.yml trigger config |
| R4 | El pipeline ejecuta secuencialmente: **Restore → Lint → Build → Test** (backend + frontend). | Spec §3 |
| R5 | `develop` y `staging` requieren **1 code review** aprobado. | branch-protection.md |
| R6 | `main` requiere **2 code reviews** aprobados + **CI exitoso**. | branch-protection.md |
| R7 | Release Please dispara en **push a `main`** y genera **tags + release notes** desde conventional commits. | release-please.yml |
| R8 | `security-review.yml` corre en **PRs a `staging` y `main`** usando `anthropics/claude-code-action@beta`. | security-review.yml |
| R9 | El ambiente dev/stg/prod se modela con las ramas `develop`/`staging`/`main`. | branch-protection.md |
| R10 | La estrategia de protección está **documentada** en `.github/branch-protection.md` (31 líneas, paso a paso). | branch-protection.md |

---

## **Discrepancias con la spec original**

| # | Spec original | Implementación real | Impacto |
|---|---|---|---|
| D1 | "Lint: Validación de estilos de código para **C#** y **ESLint** para React" | `ci.yml` solo ejecuta `npm run lint` (frontend). **No hay paso explícito de linter C#**. | 🟡 Falta `dotnet format --verify-no-changes` o equivalente en `ci.yml`. |
| D2 | "Inclusión obligatoria de la acción `claude-code-security-review` en **cada PR**" | `security-review.yml` corre solo en PRs a `staging` y `main`, **NO en PRs a `develop`**. | 🟡 Falso negativo en dev. La acción también se llama `claude-code-action@beta` (no la variante `-security-review` específica). |
| D3 | "Generación automática de *Release Notes* utilizando **IA**" | `release-please` usa `changelog-type: simple` — **genera changelog desde conventional commits, NO con IA**. | 🟢 La intención de la spec se cumple (release notes automáticas), aunque el mecanismo es deterministic, no generativo. |
| D4 | "El merge se bloquea si los tests fallan" | `ci.yml` no tiene un "merge gate" explícito — **depende de la branch protection rule** (config manual en GitHub UI). | 🟡 La regla está documentada en `branch-protection.md` línea 28-31, pero **no se aplica automáticamente** desde código. |

> **Acción recomendada (en este PR):** cerrar los gaps D1 y D2 modificando los
> workflows. Los gaps D3 y D4 son aceptables: D3 porque cumple la intención, D4
> porque la config de branch protection es por diseño manual.

---

## **Architecture**

### Estructura de archivos

```
.github/
├── branch-protection.md     ← Documento de 31 líneas con la estrategia de ramas
└── workflows/
    ├── ci.yml               ← Pipeline principal: restore + lint + build + test
    ├── release-please.yml   ← Versionado + release notes automático
    └── security-review.yml  ← Revisión de seguridad con Claude en PRs
```

### Diagrama de flujo

```
PR abierto (a develop / staging / main)
      │
      ├─→ ci.yml (siempre)
      │     1. Checkout
      │     2. Setup .NET 8 + Node 20
      │     3. dotnet restore src/backend
      │     4. npm ci (src/frontend)
      │     5. npm run lint           ← ESLint
      │     6. dotnet build src/backend --no-restore
      │     7. npm run build          ← Vite build
      │     8. dotnet test src/backend/Tests --no-build   ← xUnit
      │     9. npm test -- --run       ← Vitest
      │
      └─→ security-review.yml (solo en PRs a staging / main)
            1. Checkout
            2. anthropics/claude-code-action@beta
               (con ANTHROPIC_API_KEY)

Push a main
      │
      └─→ release-please.yml
            1. Checkout
            2. google-github-actions/release-please-action@v4
               - release-type: simple
               - changelog-type: simple
               → Crea PR de release o tag directamente
```

### Por rama

| Rama | Push directo | PR requerido | Reviews | Status checks |
|---|---|---|---|---|
| `develop` | Permitido (no recomendado) | ✅ | 1 | `ci` |
| `staging` | Permitido (no recomendado) | ✅ | 1 | `ci` |
| `main` | ❌ Prohibido | ✅ | 2 | `ci` + `security-review` |

---

## **Workflows**

### `ci.yml` (56 líneas)

**Triggers:**
- `push` a `develop`, `staging`
- `pull_request` a `develop`, `staging`, `main`

**Job `ci` (ubuntu-latest):**

| # | Step | Comando | Propósito |
|---|---|---|---|
| 1 | Checkout | `actions/checkout@v4` | Clonar el repo |
| 2 | Setup .NET 8 | `actions/setup-dotnet@v4` con `8.0.x` | Toolchain backend |
| 3 | Setup Node.js 20 | `actions/setup-node@v4` con `20.x` + `cache: 'npm'` | Toolchain frontend (con cache) |
| 4 | Restore NuGet | `dotnet restore src/backend` | Restaurar paquetes .NET |
| 5 | Install npm deps | `npm ci` (working-directory: `src/frontend`) | Install limpio desde lockfile |
| 6 | Lint frontend | `npm run lint` | ESLint sobre `src/frontend` |
| 7 | Build backend | `dotnet build src/backend --no-restore` | Compilar solución .NET |
| 8 | Build frontend | `npm run build` | Build de producción (Vite) |
| 9 | Test backend | `dotnet test src/backend/Tests --no-build` | xUnit sobre `src/backend/Tests` |
| 10 | Test frontend | `npm test -- --run` | Vitest en modo single-run |

**Tiempo estimado:** 3-5 minutos en frío, 1-2 con cache de npm.

### `release-please.yml` (22 líneas)

**Triggers:** `push` a `main`

**Job `release`:**
- Usa `google-github-actions/release-please-action@v4`
- Config: `release-type: simple`, `changelog-type: simple`
- Token: `GITHUB_TOKEN` (auto-provisionado)
- **Efecto:** cuando hay conventional commits acumuladas en `main` desde el último
  release, crea (a) un PR de release con el CHANGELOG actualizado y bump de versión
  semántica, o (b) mergea el PR y crea el tag + GitHub Release directamente.

> **Requisito:** los commits deben seguir [Conventional Commits](https://www.conventionalcommits.org/)
> (`feat:`, `fix:`, `BREAKING CHANGE:`, etc.) para que release-please pueda inferir
> el bump de versión. Ver [ADR-005](../architecture/adr/005-convenciones-naming.md).

### `security-review.yml` (19 líneas)

**Triggers:** `pull_request` a `staging`, `main`

**Job `security_review`:**
- Usa `anthropics/claude-code-action@beta`
- Requiere secret `ANTHROPIC_API_KEY` configurado en GitHub repo settings
- **Efecto:** Claude revisa el diff del PR y postea comentarios con findings
  (vulnerabilidades, secrets hardcodeados, anti-patterns, etc.).

---

## **Estrategia de Protección de Ramas**

Ver documento completo: [`.github/branch-protection.md`](../../.github/branch-protection.md)

### Resumen ejecutivo

```yaml
develop:
  require_pr: true
  required_approving_review_count: 1
  required_status_checks: [ci]

staging:
  require_pr: true
  required_approving_review_count: 1
  required_status_checks: [ci]

main:
  require_pr: true
  required_approving_review_count: 2
  required_status_checks: [ci, security-review]
  block_push_direct: true
```

### ⚠️ Aplicación manual

Las reglas de branch protection **deben configurarse manualmente** en GitHub UI:

1. `Settings > Branches > Branch protection rules > Add rule`
2. Crear una regla para `develop`, otra para `staging`, otra para `main`
3. Configurar según el resumen de arriba

**Razón:** las branch protection rules viven en GitHub repo settings, no en código
versionable. Es por diseño.

---

## **Configuración de Secrets en GitHub**

| Secret | Usado por | Cómo obtenerlo |
|---|---|---|
| `GITHUB_TOKEN` | `release-please.yml` | Auto-provisionado por GitHub Actions |
| `ANTHROPIC_API_KEY` | `security-review.yml` | Crear API key en https://console.anthropic.com y agregarla en `Settings > Secrets and variables > Actions` |

---

## **Conventional Commits — el pegamento de todo**

Sin conventional commits, release-please **no puede** inferir el bump de versión.
El proyecto usa (ver [ADR-005](../architecture/adr/005-convenciones-naming.md)):

```
feat: nueva feature                    → MINOR bump (1.2.0 → 1.3.0)
fix: bug fix                          → PATCH bump (1.2.0 → 1.2.1)
feat!: breaking change                → MAJOR bump (1.2.0 → 2.0.0)
BREAKING CHANGE: <desc> en el body    → MAJOR bump
refactor:, docs:, test:, chore:       → sin bump (aparecen en changelog)
```

> **Prohibido:** `Co-Authored-By` o cualquier atribución de IA en los commits
> (ver `AGENTS.md` sección 6 — Convenciones de commit).

---

## **Trabajo futuro (no incluido en este PR)**

1. **Cerrar gap D1 (linter C#)** — Agregar `dotnet format src/backend --verify-no-changes --no-restore` como step en `ci.yml` después del build del backend. Trabajo: 1 PR de 5 líneas.
2. **Cerrar gap D2 (security-review en develop)** — Agregar `develop` al array `branches` en `security-review.yml`. Trabajo: 1 PR de 2 líneas.
3. **Validar la branch protection real** — Una vez aplicadas las reglas en GitHub UI, verificar con un PR de prueba que los checks se ejecutan y el merge se bloquea cuando fallan.
4. **HU-14 — Despliegue local** — Hay un task abandonado en `instructions.md` (creado el 2026-04-13) que pide el archivo `docs/tasks/HU14-despliegue_local.md`. Nunca se creó. **Work item huérfano, prioridad sugerida: media** (necesario para validar el stack completo en local antes de CI/CD).
5. **Cache de NuGet** — `ci.yml` no tiene `cache` para NuGet (solo para npm). Agregar `actions/cache@v4` con key `nuget-${{ hashFiles('**/*.csproj') }}` reduciría el tiempo de CI en ~30s.
6. **Matriz de OS** — Solo se testea en `ubuntu-latest`. Si se necesita soporte Windows/macOS, expandir a `runs-on: ${{ matrix.os }}` con `[ubuntu-latest, windows-latest, macos-latest]`.

---

## **Métricas de salud del CI**

Para trackear el estado del pipeline:

- **Workflow runs:** https://github.com/<org>/gym-light/actions
- **Status check del último commit en main:** badge sugerido en `README.md` con `![CI](https://github.com/<org>/gym-light/actions/workflows/ci.yml/badge.svg?branch=main)`
- **Tasa de success** esperada: >95% (los fallos deberían ser flaky tests, no código roto, dado que la protección está activa).

---

**Implementado en:** commit `fc54d98` (2026-04-13)
**Spec:** [`docs/tasks/HU-001-HU-099/HU-013-cicd.md`](../tasks/HU-001-HU-099/HU-013-cicd.md)
**Estrategia de protección:** [`.github/branch-protection.md`](../../.github/branch-protection.md)
