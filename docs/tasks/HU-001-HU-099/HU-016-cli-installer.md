# HU-016: CLI + Installer

**Status**: 🟡 In Progress (implementación base completa)
**Owner**: @gymflow-tech-lead
**Created**: 2026-06-10
**Updated**: 2026-06-11
**Priority**: Should
**Estimación**: XL

> **HU que cierra el modelo self-hosted.** Es la interfaz de operador del sistema. Sin CLI, el operador no tiene forma de instalar, upgrade, ni diagnosticar el sistema. Complementa [HU-014 Despliegue local](docs/tasks/HU-001-HU-099/HU-014-despliegue-local.md) y consume [HU-017 Schema versioning](docs/tasks/HU-001-HU-099/HU-017-schema-versioning.md) + [HU-015 Sistema de módulos](docs/tasks/HU-001-HU-099/HU-015-sistema-de-modulos.md).

## 📦 Implementation Status

### Implemented (2026-06-11)
- ✅ `src/cli/GymFlow.Cli/` — Proyecto .NET 8
- ✅ `gymflow install` — Stub con generadores
- ✅ `gymflow status` — Funcional
- ✅ `gymflow doctor` — Checks Docker/PostgreSQL/Redis
- ✅ `gymflow module list` — Lista hardcoded
- ✅ `gymflow serve` — Wrapper Docker Compose
- ✅ `gymflow backup` — Stub pg_dump
- ✅ `gymflow restore` — Stub psql
- ⚠️ `gymflow upgrade` — **Stub (requiere HU-017)**
- ⚠️ `gymflow module enable|disable` — **Stub (requiere HU-015)**

### Code Location
```
src/cli/
├── GymFlow.Cli.sln
└── GymFlow.Cli/
    ├── GymFlow.Cli.csproj
    ├── Program.cs
    └── Commands/
        ├── InstallCommand.cs
        ├── UpgradeCommand.cs      ← Stub (HU-017 pending)
        ├── StatusCommand.cs
        ├── DoctorCommand.cs
        ├── ModuleCommand.cs   ← Stub (HU-015 pending)
        ├── ServeCommand.cs
        ├── BackupCommand.cs
        ├── RestoreCommand.cs
        └── Helpers/
            ├── DockerComposeGenerator.cs
            ├── EnvironmentGenerator.cs
            └── PrerequisitesChecker.cs
```

---

## 🎯 Intent

Proveer una interfaz de línea de comandos (CLI) unificada para que cualquier operador (vos, un sysadmin, o un cliente técnico) pueda instalar, operar, upgrade, y diagnosticar una instancia de GymFlow Lite sin necesidad de conocer los detalles internos de Docker, PostgreSQL, o la arquitectura del proyecto. El CLI es el **punto de contacto único** del operador con el sistema.

El installer inicializa una nueva instancia desde cero (genera docker-compose.yml, configura environment, corre migraciones iniciales, registra módulos base). El CLI permite operar el sistema en el día a día.

---

## 📋 Scope

### In Scope
- Binario `gymflow` con subcomandos: `install`, `upgrade`, `status`, `doctor`, `module`, `serve`, `backup`, `restore`.
- **Installer** (`gymflow install`): genera configuración, inicializa DB, aplica migraciones, registra módulos.
- **Upgrade** (`gymflow upgrade`): consume HU-017.
- **Status** (`gymflow status`): muestra versión actual, estado del schema, módulos activos, espacio en disco.
- **Doctor** (`gymflow doctor`): diagnóstico completo (DB, Redis, Docker, versión de PostgreSQL, configuración).
- **Module** (`gymflow module list|enable|disable`): gestión de módulos (HU-015).
- **Serve** (`gymflow serve`): levanta el backend + frontend localmente (conveniencia).
- **Backup/Restore** (`gymflow backup|restore`): wrappers sobre `pg_dump` y restore.
- Install puede generar `docker-compose.yml` + `.env` automáticamente (modo Docker) o instalar directo (modo native).
- Flags: `--verbose`, `--dry-run`, `--config <path>`.
- Tab-completion para bash/zsh.
- Help contextual para cada comando.

### Out of Scope
- GUI de instalación (para v1 — solo CLI).
- Install en cloud providers específicos (AWS, GCP, etc.) — futuro.
- Dashboard web de monitoreo — futuro (HU future).
- CLI de gestión de miembros (ya existe el API).

---

## 👥 User Story

**Como** operador (sysadmin o desarrollador)
**Quiero** poder instalar una nueva instancia de GymFlow Lite con 1 comando
**Para** no leer 10 páginas de documentación cada vez que necesito desplegar

**Como** operador en producción
**Quiero** poder hacer upgrade, backup, y diagnóstico con comandos simples
**Para** mantener el sistema funcionando sin ser un expert en Docker o PostgreSQL

**Como** cliente técnico que se autogestiona
**Quiero** poder habilitar/disable módulos y ver el estado del sistema
**Para** tener control sobre mi instancia sin depender del equipo de desarrollo

**Como** yo (developer) desarrollando features
**Quiero** poder levantar el entorno local con `gymflow serve`
**Para**no necesito saber cómo funcionan Docker ni la configuración exacta de cada servicio

---

## ✅ Requirements

### MUST (instalación)
- [ ] `gymflow install --name "MiGym" --url "https://gimnasio.example.com"` genera `docker-compose.yml` + `.env` funcionales.
- [ ] El install puede hacerse en modo Docker (`--mode docker`) o nativo (`--mode native`).
- [ ] En modo Docker, el install detecta si Docker y Docker Compose están disponibles (sino error claro).
- [ ] En modo Docker, genera volumenes persistentes para PostgreSQL y Redis.
- [ ] El install inicializa la DB con migraciones (HU-017).
- [ ] El install registra los módulos base (Core, Members, Sales, Freeze, Cancellation, Anthropometry, Routines — sin Planning ni Metrics).
- [ ] El install muestra las credenciales generadas (password de DB, secrets) y las guarda en `.env.gymflow` (fuera del repo).
- [ ] El install detecta conflictos de puerto (si 5432 o 6379 están ocupados).
- [ ] El install tiene flag `--dry-run` que muestra qué haría sin hacerlo.

### MUST (operación)
- [ ] `gymflow upgrade --target 1.x.0` aplica el upgrade de schema (HU-017).
- [ ] `gymflow status` muestra: versión del CLI, versión del schema, última migración, módulos activos, uso de disco, URLs de servicios.
- [ ] `gymflow doctor` hace diagnóstico completo y sale con código 0 si todo está bien, >0 si hay problemas.
- [ ] `gymflow module list` muestra todos los módulos disponibles (del assembly scan).
- [ ] `gymflow module enable <name>` registra el módulo y aplica sus migraciones.
- [ ] `gymflow module disable <name>` marca el módulo como inactivo (no borra data).
- [ ] `gymflow backup` hace `pg_dump` y guarda en `backups/` con timestamp.
- [ ] `gymflow restore <backup-file>` restaura desde un backup (requiere confirmación).
- [ ] `gymflow serve` levanta todos los servicios localmente (DB + Redis + backend + frontend).
- [ ] Todos los comandos tienen `--verbose` para mostrar logs detallados.
- [ ] Todos los comandos tienen `--help` con ejemplos.

### SHOULD (calidad)
- [ ] Tab-completion para bash y zsh.
- [ ] Output con colores (verde/rojo/amarillo según severidad).
- [ ] Progress bars para operaciones largas (install, upgrade, backup).
- [ ] El CLI pregunta confirmación antes de operaciones destructivas (restore, disable módulo con data).
- [ ] Los secretos nunca se imprimen en la terminal (solo en archivos).
- [ ] El CLI detecta si está corriendo dentro de un proyecto GymFlow ya instalado o fuera.

### COULD (futuro)
- [ ] `gymflow install --cloud aws|gcp|azure` (futuro).
- [ ] `gymflow logs` (tail de logs de todos los servicios).
- [ ] `gymflow metrics` (muestra métricas básicas de uso).

---

## 🧪 Criterios de Aceptación (Given/When/Then)

- [ ] **Given** un servidor vacío con Docker instalado
      **When** el operador corre `curl -sSL https://get.gymflow.io/install.sh | gymflow install --name "Gimnasio XYZ" --mode docker`
      **Then** en <5 min tiene una instancia corriendo con docker-compose.yml, .env configurado, DB inicializada, módulos base activos, y la URL del frontend visible

- [ ] **Given** una instancia corriendo en v1.0
      **When** el operador corre `gymflow upgrade --target 1.1.0`
      **Then** el sistema hace backup automático, aplica migraciones, muestra "Upgrade successful: v1.0 → v1.1.0" con duración

- [ ] **Given** el operador corre `gymflow doctor`
      **When** la DB está sana, Redis responde, Docker está OK
      **Then** la salida muestra todos los checks en verde con "✅ All checks passed" y exit code 0

- [ ] **Given** el operador corre `gymflow doctor` y la DB tiene un problema
      **When** detecta que la versión de PostgreSQL es incompatible
      **Then** muestra "❌ PostgreSQL version mismatch: found 12, need 14+" y sugiere cómo arreglarlo

- [ ] **Given** el operador corre `gymflow module list`
      **When** hay 7 módulos registrados (base)
      **Then** muestra tabla con: nombre, versión, estado (active/inactive), última migración

- [ ] **Given** el operador corre `gymflow module enable planning`
      **When** el módulo Planning existe en el assembly
      **Then** registra el módulo, aplica sus migraciones, actualiza instance_settings, muestra "Module 'planning' enabled"

- [ ] **Given** el operador corre `gymflow backup`
      **When** el backup se genera
      **Then** el archivo queda en `backups/gymflow-pre-v1.1.0-20260610-153045.sql` y la salida dice "Backup created: backups/gymflow-pre-..."

- [ ] **Given** el operador corre `gymflow serve`
      **When** todos los servicios están disponibles
      **Then** la salida muestra las URLs: Backend http://localhost:5000, Frontend http://localhost:3000, API http://localhost:5000/api

- [ ] **Given** el operador corre cualquier comando con `--dry-run`
      **When** el comando es install o upgrade
      **Then** no modifica nada, solo muestra qué haría (archivos, comandos Docker, migraciones)

---

## 🔗 Dependencias

- **Depende de**:
  - [HU-014 Despliegue local](docs/tasks/HU-001-HU-099/HU-014-despliegue-local.md) — el install usa Docker Compose existente
  - [HU-017 Schema versioning](docs/tasks/HU-001-HU-099/HU-017-schema-versioning.md) — `gymflow upgrade` consume el SchemaUpgrader
  - [HU-015 Sistema de módulos](docs/tasks/HU-001-HU-099/HU-015-sistema-de-modulos.md) — `gymflow module list|enable|disable` consume el ModuleRegistry
  - PostgreSQL client (`psql`, `pg_dump`) para backup/restore
  - Docker SDK o wrapper sobre `docker compose` para comandos Docker

- **Bloquea**:
  - El "flow de operador completo" del modelo self-hosted
  - La promesa de "1 comando para instalar" que se usa en marketing (cuando sea relevant)

---

## 📦 Affected Areas

### CLI (nuevo proyecto)
```
src/cli/
├── GymFlow.Cli/                    # proyecto .NET
│   ├── Program.cs                   # entry point, parsea comandos
│   ├── Commands/
│   │   ├── InstallCommand.cs        # gymflow install
│   │   ├── UpgradeCommand.cs        # gymflow upgrade
│   │   ├── StatusCommand.cs        # gymflow status
│   │   ├── DoctorCommand.cs        # gymflow doctor
│   │   ├── ModuleCommand.cs         # gymflow module list|enable|disable
│   │   ├── ServeCommand.cs          # gymflow serve
│   │   ├── BackupCommand.cs         # gymflow backup
│   │   └── RestoreCommand.cs       # gymflow restore
│   ├── Helpers/
│   │   ├── DockerComposeGenerator.cs
│   │   ├── EnvironmentGenerator.cs
│   │   ├── BackupHelper.cs          # wrapper sobre pg_dump
│   │   ├── DoctorChecker.cs         # checks de diagnóstico
│   │   └── ModuleHelper.cs
│   ├── Services/
│   │   ├── ISchemaService.cs        # consume SchemaUpgrader (HU-017)
│   │   ├── IModuleService.cs        # consume ModuleRegistry (HU-015)
│   │   └── IInstallerService.cs
│   └── Infrastructure/
│       └── GymFlow.Cli.Infrastructure.csproj
├── GymFlow.Cli.sln
└── README.md                        # documentación del CLI
```

### Backend (extensiones)
- `SchemaUpgrader` (HU-017) se expone como servicio consumible por el CLI
- `ModuleRegistry` (HU-015) se expone como servicio consumible por el CLI
- `instance_settings` JSONB se usa para guardar estado del CLI (último backup, lock de upgrade)

### Docs
- `docs/technical/cli-reference.md` (nueva — referencia completa de todos los comandos)
- `docs/technical/upgrade-guide.md` (HU-017 — referenciada por `gymflow upgrade`)
- `docs/technical/installation-guide.md` (nueva — guía de instalación paso a paso, referenciada por `gymflow install --help`)
- `docs/templates/onboarding/template-onboarding.md` (actualizado — cómo instalar desde cero)

### Scripts
- `scripts/check-migration-policy.py` (HU-017 — el linter que el CLI invoca)

### Tests
- `src/cli/Tests/Commands/InstallCommandTests.cs`
- `src/cli/Tests/Commands/UpgradeCommandTests.cs`
- `src/cli/Tests/Commands/DoctorCommandTests.cs`
- `src/cli/Tests/Helpers/DockerComposeGeneratorTests.cs`

---

## 🧪 Verification

- [ ] `gymflow install --dry-run` genera archivos de configuración sin modificar nada
- [ ] `gymflow install --name "TestGym" --mode docker` genera docker-compose.yml funcional
- [ ] `gymflow status` muestra información correcta (versión, schema, módulos)
- [ ] `gymflow doctor` pasa todos los checks en entorno sano
- [ ] `gymflow doctor` detecta PostgreSQL viejo o Redis abajo
- [ ] `gymflow module list` muestra los módulos registrados
- [ ] `gymflow module enable <module>` registra el módulo y actualiza instance_settings
- [ ] `gymflow module disable <module>` marca inactivo sin borrar data
- [ ] `gymflow upgrade --target 1.1.0 --dry-run` muestra las migraciones que aplicaría
- [ ] `gymflow backup` genera archivo SQL en `backups/`
- [ ] `gymflow restore <file>` restaura correctamente (con confirmación)
- [ ] `gymflow serve` levanta todos los servicios localmente
- [ ] Tab-completion funciona en bash y zsh
- [ ] `--help` muestra ejemplos para cada comando

---

## 📝 Notas

### Modelo de instalación

El CLI tiene dos modos de instalación:

**Modo Docker (default)**:
```bash
gymflow install --name "MiGym" --mode docker
# Genera:
#   docker-compose.yml        (servicios: postgres, redis, backend, frontend)
#   .env                      (credenciales, ports, URLs)
#   .env.gymflow              (secretos, fuera del repo — .gitignore)
```

**Modo Native** (para desarrollo o servers donde Docker no está disponible):
```bash
gymflow install --name "MiGym" --mode native
# Genera:
#   config/
#     appsettings.json        (configuración de la API)
#   init-db.sql               (script de inicialización)
#   run.sh                    (script para levantar servicios)
```

### Distribución del CLI

El CLI se distribuye como:
1. **Binario standalone**: `gymflow` (Linux amd64, macOS arm64, Windows). Descarga desde GitHub Releases.
2. **Install script**:
   ```bash
   curl -sSL https://get.gymflow.io/install.sh | bash
   ```
   El script detecta el OS, descarga el binario correcto, lo hace ejecutable, y lo agrega al PATH.

### Arquitectura del CLI

El CLI es un proyecto .NET separado (`src/cli/GymFlow.Cli`) que referencia los proyectos core del backend. No tiene lógica de negocio propia — delega a servicios del backend:

```
CLI Commands → CLI Services (ISchemaService, IModuleService) → Backend Core (SchemaUpgrader, ModuleRegistry)
```

Esto evita duplicar lógica. Los tests del CLI pueden mockear los servicios del backend.

### Comandos que integran otras HUs

| Comando | HU que provee la lógica |
|---|---|
| `gymflow upgrade` | [HU-017](docs/tasks/HU-001-HU-099/HU-017-schema-versioning.md) |
| `gymflow status` | [HU-017](docs/tasks/HU-001-HU-099/HU-017-schema-versioning.md) |
| `gymflow doctor` | [HU-017](docs/tasks/HU-001-HU-099/HU-017-schema-versioning.md) |
| `gymflow module list\|enable\|disable` | [HU-015](docs/tasks/HU-001-HU-099/HU-015-sistema-de-modulos.md) |
| `gymflow serve` | [HU-014](docs/tasks/HU-001-HU-099/HU-014-despliegue-local.md) |
| `gymflow install` | Propio + usa docker-compose.yml de HU-014 |

### Roadmap de features

| Feature | Prioridad | Release target |
|---|---|---|
| Comandos base (install, upgrade, status, doctor) | Must | v1.1 |
| Module management | Should | v1.1 |
| Backup/Restore | Should | v1.1 |
| Serve | Should | v1.1 |
| Tab-completion | Could | v1.2 |
| Install en cloud (AWS/GCP) | Won't | v2+ |
| Dashboard web de monitoreo | Won't | v2+ |

---

# Ciclo SDD (consolidado)

## 1. Proposal (resumido)

El CLI es el punto de contacto del operador con el sistema. Sin él, el modelo self-hosted no es accesible para operadores no-técnicos. Necesita ser simple, robusto, y cubrir todo el ciclo de vida (install → operate → upgrade → diagnose).

## 2. Spec (resumida)

- Binario `gymflow` con 8 subcomandos principales.
- Modo Docker (genera docker-compose.yml) y modo Native.
- Consume servicios del backend (SchemaUpgrader, ModuleRegistry) sin duplicar lógica.
- Help contextual, dry-run, verbose flags en todos los comandos.
- Distribución via install script + GitHub Releases.

## 3. Design (resumido)

### Comandos principales

```
gymflow --help
gymflow install --name <name> [--mode docker|native] [--url <url>] [--dry-run]
gymflow upgrade --target <version> [--dry-run] [--skip-backup]
gymflow status [--verbose]
gymflow doctor [--fix]
gymflow module list|enable <name>|disable <name>
gymflow serve [--port <port>]
gymflow backup [--output <path>]
gymflow restore <backup-file> [--confirm]
```

### Flujo de install

```
1. Validar prerrequisitos (Docker, psql, pg_dump)
2. Generar config files (docker-compose.yml, .env, .env.gymflow)
3. Validar puertos disponibles
4. Crear directorios (backups/, logs/, data/)
5. Hacer primer backup (vacío, solo para establecer el pattern)
6. Inicializar DB (psql + migrate)
7. Registrar módulos base (ModuleRegistry)
8. Mostrar resumen + credenciales
```

### Flujo de upgrade (consume HU-017)

```
1. acquire_lock()
2. pre_upgrade_checks()
3. backup()
4. apply_migrations()
5. verify()
6. release_lock()
7. log_summary()
```

### Arquitectura de servicios CLI → Backend

```
GymFlow.Cli/
├── ISchemaService          → Backend: SchemaUpgrader
├── IModuleService          → Backend: ModuleRegistry
├── IInstallerService      → Propio (generación de archivos)
├── IBackupService          → Wrapper sobre pg_dump
└── IDoctorService          → Propio (checks)
```

## 4. Tasks (desglose)

### Batch A — CLI core + install (2 sprints)

1. **A1** — Crear proyecto `src/cli/GymFlow.Cli/` con estructura de comandos (3 días)
2. **A2** — Implementar `InstallCommand` con generador de docker-compose.yml (4 días)
3. **A3** — Implementar generador de `.env` con secretos seguros (2 días)
4. **A4** — Implementar validación de prerrequisitos (Docker, psql, pg_dump) (2 días)
5. **A5** — Implementar inicialización de DB (llama migraciones) (2 días)
6. **A6** — Tests de `InstallCommand` (3 días)
7. **A7** — Docs: `installation-guide.md` (1 día)

**Criterio de fin Batch A**: `gymflow install --dry-run` genera archivos; `gymflow install` en modo Docker levanta la instancia.

### Batch B — Upgrade + Status + Doctor (1.5 sprints)

1. **B1** — Consumir `SchemaUpgrader` (HU-017) desde el CLI (2 días)
2. **B2** — Implementar `UpgradeCommand` con pre-checks + backup (3 días)
3. **B3** — Implementar `StatusCommand` (1 día)
4. **B4** — Implementar `DoctorCommand` con checks de salud (3 días)
5. **B5** — Tests de upgrade con DB de test (3 días)

**Criterio de fin Batch B**: `gymflow upgrade`, `gymflow status`, `gymflow doctor` funcionan.

### Batch C — Módulos + Serve + Backup/Restore (1 sprint)

1. **C1** — Consumir `ModuleRegistry` (HU-015) desde el CLI (2 días)
2. **C2** — Implementar `ModuleCommand` list|enable|disable (2 días)
3. **C3** — Implementar `ServeCommand` (wrapper sobre docker compose) (1 día)
4. **C4** — Implementar `BackupCommand` + `RestoreCommand` (2 días)
5. **C5** — Tests de Backup/Restore (2 días)

**Criterio de fin Batch C**: todos los comandos listados en Requirements funcionan.

### Batch D — Polish + Distribución (0.5 sprint)

1. **D1** — Tab-completion para bash/zsh (2 días)
2. **D2** — Install script (`curl -sSL https://get.gymflow.io/install.sh | bash`) (1 día)
3. **D3** — GitHub Actions: build + release para cada OS (1 día)
4. **D4** — Docs: `cli-reference.md` (1 día)

**Criterio de fin Batch D**: binario disponible para descarga, install script funcional.

---

## 🔗 Referencias

- [HU-014 Despliegue local](docs/tasks/HU-001-HU-099/HU-014-despliegue-local.md) — docker-compose.yml que el CLI usa
- [HU-015 Sistema de módulos](docs/tasks/HU-001-HU-099/HU-015-sistema-de-modulos.md) — ModuleRegistry que el CLI consume
- [HU-017 Schema versioning](docs/tasks/HU-001-HU-099/HU-017-schema-versioning.md) — SchemaUpgrader que el CLI consume
- [ADR-007 Self-Hosted](docs/architecture/adr/007-modelo-self-hosted.md) — modelo de distribución que el CLI habilita
- [RFC-002 §6](docs/architecture/rfc/002-modelo-de-negocio-y-gobernanza.md#6-roadmap-estratégico) — lugar en el roadmap
- [template-hu-sdd.md](docs/templates/hu/template-hu-sdd.md) — template usado
- Odoo, Supabase, Cal.com — precedentes de CLI para self-hosted