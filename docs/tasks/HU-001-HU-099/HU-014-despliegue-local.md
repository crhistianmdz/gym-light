# HU-014: Despliegue Local

**Status**: 🟡 In Progress
**Owner**: @gymflow-tech-lead
**Created**: 2026-06-10
**Priority**: Should
**Estimación**: M

> **Nota histórica**: Esta HU fue creada originalmente el 2026-04-13 (marcada en `instructions.md`) pero el archivo nunca se materializó. Se retoma acá como parte de la Fase C del roadmap ([RFC-002 §6](docs/architecture/rfc/002-modelo-de-negocio-y-gobernanza.md#6-roadmap-estratégico)). El stack Docker base YA EXISTE y funciona (commits `c1edf73` y `338c2e7`), lo que falta es la **experiencia de instalación pulida** y la **documentación de dev local**.

---

## 🎯 Intent

Facilitar que un dev (interno o contribuidor externo) pueda clonar el repo y tener GymFlow Lite corriendo en su máquina local en menos de 15 minutos, sin pelearse con versiones de .NET, npm, ni particularidades del OS. La instalación debe ser reproducible y destructible (un comando para armar, un comando para desarmar).

---

## 📋 Scope

### In Scope
- Verificar y pulir el `docker-compose.yml` existente (4 servicios: backend, frontend, postgres, redis).
- Documentación de instalación paso a paso para Linux, macOS, Windows (WSL2).
- Scripts de utilidad para dev (start, stop, reset, logs, seed).
- Validación de que el frontend (Vite/React) habla con el backend (.NET 8) end-to-end.
- Validación de que la DB persiste entre reinicios (volúmenes Docker).
- `.env.example` documentado con todas las variables necesarias.
- Troubleshooting de los errores más comunes (puertos ocupados, permisos, etc.).

### Out of Scope
- Despliegue a producción (eso es otra HU/proceso, ver [HU-016 CLI + installer](docs/tasks/HU-001-HU-099/HU-016-cli-installer.md) que cubre self-hosted en prod).
- HTTPS / dominio / SSL (eso es setup de prod).
- Backups automatizados (eso es prod, ver [RFC-002 §6 horizonte 2](docs/architecture/rfc/002-modelo-de-negocio-y-gobernanza.md#6-roadmap-estratégico)).
- CI/CD improvements (eso es HU-13, ya implementada).

---

## 👥 User Story

**Como** desarrollador que quiere contribuir a GymFlow Lite
**Quiero** clonar el repo y tener todo el stack corriendo localmente en menos de 15 minutos
**Para** empezar a贡献 código, docs, o tests sin pelearme con setup

**Como** tester o curioso que quiere probar el producto
**Quiero** instalarlo en mi máquina sin conocimientos técnicos avanzados
**Para** ver cómo funciona antes de comprometer mi tiempo o mi data

---

## ✅ Requirements

### MUST (obligatorios)
- [ ] Un solo comando (`docker compose up -d`) levanta los 4 servicios healthy.
- [ ] El frontend sirve en `http://localhost:3000` y muestra la UI de login.
- [ ] El backend responde en `http://localhost:5000/health` con 200 OK.
- [ ] La DB persiste entre reinicios (volúmenes Docker).
- [ ] Las migraciones de EF Core se aplican automáticamente al primer arranque (solo dev).
- [ ] Hay un seed con datos de prueba (1 gimnasio, 1 admin, 10 socios, 5 productos).
- [ ] README.md tiene una sección "Local Setup" paso a paso con copy-paste-ready.
- [ ] `docker compose down -v` borra TODO (DB incluida) para empezar de cero.

### SHOULD (importantes, no bloqueantes)
- [ ] `.env.example` documenta todas las variables de entorno necesarias.
- [ ] Hay un `Makefile` (o equivalente) con targets: `make dev`, `make stop`, `make reset`, `make logs`, `make seed`.
- [ ] Hay un script `scripts/doctor.sh` que valida que Docker, Docker Compose, puertos libres, etc. estén OK antes de empezar.
- [ ] La sección de Troubleshooting cubre al menos: puertos ocupados, error de migraciones, error de permisos en volúmenes.
- [ ] El primer `docker compose up` tarda menos de 5 minutos (incluyendo build de imágenes).

### COULD (deseables, futuro)
- [ ] Hot-reload del backend (dotnet watch dentro del container).
- [ ] Hot-reload del frontend (Vite HMR ya lo tiene, pero documentar bien).
- [ ] Script para abrir VSCode con dev containers preconfigurados.
- [ ] Documentación de cómo testear con datos limpios vs datos sembrados.

---

## 🧪 Criterios de Aceptación

- [ ] **Given** un dev con Docker 24+ y Docker Compose v2+ instalados, en Linux/macOS/Windows WSL2
      **When** ejecuta `git clone <repo> && cd gymflow && docker compose up -d`
      **Then** en menos de 5 minutos los 4 servicios están `healthy` y `http://localhost:3000` muestra la UI de login

- [ ] **Given** el stack está corriendo y el dev hace cambios en código del backend
      **When** el dev quiere ver los cambios
      **Then** el backend recarga automáticamente (dotnet watch) sin perder la sesión

- [ ] **Given** el dev terminó de probar y quiere empezar de cero
      **When** ejecuta `docker compose down -v && docker compose up -d`
      **Then** la DB se borra, las migraciones se reaplican, el seed se vuelve a correr, todo limpio

- [ ] **Given** un dev nuevo en el proyecto sin experiencia previa
      **When** sigue el README paso a paso
      **Then** puede contribuir un PR en menos de 1 hora desde el clone

---

## 🔗 Dependencias

- **Depende de**:
  - Docker + Docker Compose instalados (prerrequisito del entorno, no del proyecto)
  - Git (prerrequisito)
- **Bloquea**:
  - Onboarding de contribuidores externos (sin dev local funcional, no hay贡献)
  - [HU-016 CLI + installer](docs/tasks/HU-001-HU-099/HU-016-cli-installer.md) (parcialmente, comparten la base de Docker)
- **Relacionado con**:
  - [HU-13 CI/CD](docs/technical/hu13-cicd.md) — CI usa Docker similar

---

## 📦 Affected Areas

- `docker/docker-compose.yml` — verificar, pulir, documentar
- `docker/backend/Dockerfile` — verificar
- `docker/frontend/Dockerfile` — verificar (puede ser nginx o similar)
- `docker/.env.example` — crear/actualizar
- `scripts/doctor.sh` — crear (validación previa al setup)
- `Makefile` — crear con targets útiles
- `README.md` — agregar sección "Local Setup"
- `docs/technical/local-setup.md` — crear guía detallada (referenciada desde README)
- `docs/architecture/adr/001-stack-tecnologico.md` — sin cambios (ya menciona Docker)

---

## 🧪 Verification

- [ ] Script `scripts/doctor.sh` pasa en Linux limpio
- [ ] Script `scripts/doctor.sh` pasa en macOS limpio
- [ ] Script `scripts/doctor.sh` pasa en Windows con WSL2
- [ ] `docker compose up -d` levanta los 4 servicios healthy en <5 min
- [ ] Login funciona con las credenciales del seed
- [ ] Check-in funciona end-to-end (crear socio → asignar membresía → check-in)
- [ ] Sync offline-first funciona (desconectar wifi → check-in → reconectar → ver log en server)
- [ ] Reset (`docker compose down -v`) deja el sistema en estado limpio
- [ ] Tests unitarios pasan dentro del container

---

## 📝 Notas

### Contexto histórico

El stack Docker YA EXISTE (commits `c1edf73` Docker setup + `338c2e7` Refactor Dockerfile). Lo que falta es pulirlo y documentarlo bien. Esta HU es de **CIERRE**, no de implementación desde cero.

### Decisiones tomadas

1. **Docker Compose como método primario** (no Kubernetes, no Podman) — es lo más simple para dev local y 1 dev lo puede mantener.
2. **Hot-reload via volumen mount** — el código fuente se monta como volumen, el proceso usa `dotnet watch` o Vite HMR.
3. **Seed con datos sintéticos** — un gimnasio ficticio "DemoGym" con 1 admin (`admin@demo.com / admin123`), 10 socios, 5 productos. Suficiente para probar todas las features.
4. **NO** se incluye el frontend compilado en producción en el container de dev — se sirve via Vite dev server con HMR.

### Diferencias con la implementación actual

El stack actual puede tener pequeñas inconsistencias (puertos, nombres de servicios, paths). Hay que armonizar todo. El testeo end-to-end con `dotnet watch` puede no estar activo — hay que verificarlo y activarlo.

### Riesgos identificados

| Riesgo | Mitigación |
|---|---|
| WSL2 + Docker Desktop tiene quirks de volumen mount | Documentar workarounds conocidos |
| `dotnet watch` no funciona bien en Linux containers (file watcher) | Usar `DOTNET_USE_POLLING_FILE_WATCHER=true` |
| El primer build de la imagen tarda mucho | Documentar (3-5 min es normal para .NET 8 SDK) |
| Puertos 3000/5000 ocupados | Documentar cómo cambiarlos via `.env` |

---

## 🔗 Referencias

- [Docker Compose docs](https://docs.docker.com/compose/)
- [Dockerfile best practices](https://docs.docker.com/develop/develop-images/dockerfile_best-practices/)
- [.NET 8 Docker images](https://hub.docker.com/_/microsoft-dotnet-sdk)
- [Vite dev server](https://vitejs.dev/config/server-options.html)
- [ADR-001 Stack](docs/architecture/adr/001-stack-tecnologico.md) — el stack que se deploya
- [ADR-003 Migraciones](docs/architecture/adr/003-estrategia-migraciones.md) — el auto-apply en dev
- [RFC-002 §6 Horizonte 1](docs/architecture/rfc/002-modelo-de-negocio-y-gobernanza.md#6-roadmap-estratégico) — lugar en el roadmap
- [HU-13 CI/CD](docs/technical/hu13-cicd.md) — CI usa Docker similar
- [template-hu-simple.md](docs/templates/hu/template-hu-simple.md) — template usado
