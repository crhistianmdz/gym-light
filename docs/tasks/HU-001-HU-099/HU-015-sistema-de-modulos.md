# HU-015: Sistema de Módulos / Plugins Opt-In

**Status**: 🟡 In Progress
**Owner**: @gymflow-tech-lead
**Created**: 2026-06-10
**Priority**: Must
**Estimación**: XL

> **HU grande (cambio arquitectónico)**. Esta HU implementa la decisión de [ADR-008](docs/architecture/adr/008-arquitectura-de-modulos.md). Sigue el ciclo SDD completo con proposal + spec + design + tasks al final de este archivo.

---

## 🎯 Intent

Materializar la visión del maintainer ("el cliente decide si usa o no") mediante un sistema de módulos/plugins que permita a cada instancia de GymFlow Lite activar/desactivar features individualmente. Es la pieza arquitectónica más importante del proyecto después del core: habilita plugins de terceros, facilita upgrades seguros, y permite que el producto crezca sin agrandar el core indefinidamente.

---

## 📋 Scope

### In Scope
- Interfaz `IModule` y contrato del módulo.
- Module Loader: auto-descubrimiento vía reflection + carga de assemblies.
- Module Registry: estado en memoria de qué módulos están activos.
- Persistencia: tabla `instance_settings` con JSONB `enabled_modules`.
- Panel de admin UI: listar módulos, activar/desactivar.
- Refactor de las 12 HUs existentes (HU-01 a HU-12) en módulos opt-in.
- Migraciones de DB específicas por módulo (colabora con [HU-017](docs/tasks/HU-001-HU-099/HU-017-schema-versioning.md)).
- Documentación: "Cómo crear un módulo" para futuros contribuidores.
- Tests: unit + integration del loader, del registry, y de cada módulo.

### Out of Scope
- Carga de DLLs externos desde carpeta `plugins/` (v2 — plugins de terceros).
- Hot-reload sin restart (v2 — signal-based reload).
- Marketplace de plugins (v3, RFC-002 horizonte 3).
- Plugin versioning semántico (v2).
- Sandbox de seguridad para plugins externos (v2).

---

## 👥 User Story

**Como** Owner de un gimnasio
**Quiero** poder activar o desactivar las features que no me sirven
**Para** tener una app limpia, sin opciones que confunden a mi equipo

**Como** Admin de un gimnasio
**Quiero** ver qué módulos están activos en mi instancia
**Para** diagnosticar problemas o entender qué está pasando

**Como** desarrollador que quiere agregar un módulo nuevo
**Quiero** una interfaz clara y documentación de cómo hacerlo
**Para** contribuir sin tener que entender todo el core

---

## ✅ Requirements

### MUST (obligatorios)
- [ ] Existe la interfaz `IModule` con métodos: `Name`, `Version`, `DisplayName`, `Description`, `RegisterServices`, `RegisterEndpoints`, `GetMigrations`.
- [ ] El Module Loader descubre todos los `IModule` en assemblies referenciados via reflection al startup.
- [ ] Module Registry mantiene estado en memoria de módulos activos/inactivos.
- [ ] Tabla `instance_settings` persiste los flags `enabled_modules` como JSONB.
- [ ] Panel de admin en frontend lista los módulos disponibles con toggle on/off.
- [ ] Las 12 HUs existentes (HU-01 a HU-12) se refactorizan en módulos que cumplen `IModule`.
- [ ] Activar/desactivar un módulo requiere restart del backend (v1 — sin hot-reload).
- [ ] Al startup, los módulos activos registran sus servicios y endpoints.
- [ ] Al startup, las migraciones pendientes de los módulos activos se ejecutan (en orden de dependencias entre módulos).
- [ ] Documentación `docs/technical/how-to-create-a-module.md` con ejemplo end-to-end.
- [ ] Tests unitarios del Module Loader (descubrimiento, ordering, error handling).
- [ ] Tests de integración: instancia con módulos A+B activos vs solo A activo.
- [ ] El core NO depende de ningún módulo específico (inversión de dependencias).

### SHOULD (importantes)
- [ ] Module Manifest (`module.json` o atributo `[ModuleMetadata]`) para metadatos declarativos.
- [ ] Validación de dependencias entre módulos: si A depende de B, A no se activa sin B.
- [ ] Healthcheck de cada módulo (endpoint `/health/modules/{name}`).
- [ ] Logs estructurados al cargar/activar/desactivar módulos.
- [ ] Documentación de los hooks disponibles (OnAppStartup, OnAppShutdown, OnRequestBegin, etc.).

### COULD (deseables, futuro)
- [ ] Versionado semántico de módulos: si v1.5 está activo y se carga v1.6, no se rompe.
- [ ] Permisos granulares: módulo X solo para rol Admin.
- [ ] UI muestra "qué hace" cada módulo (descripción + screenshots).
- [ ] Métricas de uso: cuántas requests a cada módulo.

---

## 🧪 Criterios de Aceptación (Given/When/Then)

- [ ] **Given** una instancia nueva con `instance_settings.enabled_modules = []`
      **When** el backend arranca
      **Then** ningún módulo se carga, los endpoints no existen, pero el core funciona (login, registro, etc.)

- [ ] **Given** una instancia con `enabled_modules = ["checkin", "sales"]`
      **When** el backend arranca
      **Then** solo los módulos "checkin" y "sales" registran servicios y endpoints. El resto está en disco pero inactivo.

- [ ] **Given** el Owner va al panel de admin → Módulos
      **When** activa "metrics" y guarda
      **Then** la próxima vez que el backend reinicie, el módulo metrics se carga y aparece en el menú de navegación.

- [ ] **Given** un dev quiere crear un módulo nuevo "bookings" (reservas de clases)
      **When** sigue `docs/technical/how-to-create-a-module.md`
      **Then** puede crear el módulo, implementarlo, y activarlo en menos de 2 horas sin tocar el core.

- [ ] **Given** dos instancias de GymFlow Lite, una con módulo "routines" activo y otra sin él
      **When** un dev hace un PR que toca el core (no el módulo routines)
      **Then** ambos casos compilan y los tests pasan (la ausencia del módulo no rompe nada).

- [ ] **Given** un módulo tiene una migración de DB que falla al aplicarse
      **When** el backend arranca
      **Then** el sistema hace rollback de la migración, loguea el error claramente, y el módulo queda en estado "error" (no "active"). El resto de la app sigue funcionando.

---

## 🔗 Dependencias

- **Depende de**:
  - [ADR-008](docs/architecture/adr/008-arquitectura-de-modulos.md) — la decisión arquitectónica
  - .NET 8 reflection APIs
  - EF Core 8 (para las migraciones de módulos)
- **Bloquea**:
  - [HU-017 Schema versioning](docs/tasks/HU-001-HU-099/HU-017-schema-versioning.md) — depende de que los módulos existan
  - [HU-016 CLI + installer](docs/tasks/HU-001-HU-099/HU-016-cli-installer.md) — el CLI aprovecha la API de módulos
- **Refactoriza**:
  - HU-01 a HU-12 → cada una se convierte en módulo

---

## 📦 Affected Areas

### Backend (nuevo)
- `src/backend/GymFlow.Core/Modules/IModule.cs`
- `src/backend/GymFlow.Core/Modules/ModuleManifest.cs`
- `src/backend/GymFlow.Core/Modules/ModuleContext.cs`
- `src/backend/GymFlow.Core/Modules/ModuleLoader.cs`
- `src/backend/GymFlow.Core/Modules/ModuleRegistry.cs`
- `src/backend/Infrastructure/Modules/ModuleSettingsStore.cs`
- `src/backend/Infrastructure/Persistence/Migrations/AddModuleSystem.cs`

### Backend (refactor)
- `src/backend/Domain/Modules/` (nuevo, contiene entidades de módulos si se necesitan)
- `src/backend/Application/UseCases/Modules/GetAvailableModulesUseCase.cs`
- `src/backend/Application/UseCases/Modules/EnableModuleUseCase.cs`
- `src/backend/Application/UseCases/Modules/DisableModuleUseCase.cs`
- `src/backend/WebAPI/Controllers/Admin/ModulesController.cs`

### Módulos refactorizados
- `src/backend/Modules/GymFlow.Module.Checkin/` (de HU-01)
- `src/backend/Modules/GymFlow.Module.Members/` (de HU-02)
- `src/backend/Modules/GymFlow.Module.Sales/` (de HU-03)
- `src/backend/Modules/GymFlow.Module.Sync/` (de HU-04) — probablemente queda en el core por ser cross-cutting
- `src/backend/Modules/GymFlow.Module.Auth/` (de HU-05) — queda en el core
- `src/backend/Modules/GymFlow.Module.Audit/` (de HU-06)
- `src/backend/Modules/GymFlow.Module.Freeze/` (de HU-07)
- `src/backend/Modules/GymFlow.Module.Cancellation/` (de HU-08)
- `src/backend/Modules/GymFlow.Module.Anthropometry/` (de HU-09)
- `src/backend/Modules/GymFlow.Module.ProgressChart/` (de HU-10)
- `src/backend/Modules/GymFlow.Module.Routines/` (de HU-11)
- `src/backend/Modules/GymFlow.Module.Metrics/` (de HU-12)

### Frontend
- `src/frontend/src/pages/admin/ModulesPage.tsx` (nueva)
- `src/frontend/src/components/admin/ModuleCard.tsx`
- `src/frontend/src/services/moduleService.ts`
- `src/frontend/src/hooks/useEnabledModules.ts`

### Docs
- `docs/technical/how-to-create-a-module.md` (nueva, ~100 líneas con ejemplo end-to-end)
- `docs/architecture/adr/008-arquitectura-de-modulos.md` (ya creado)
- Cada `huNN-*.md` debe actualizarse para mencionar que ahora es un módulo opt-in

### Tests
- `src/backend/Tests/Modules/ModuleLoaderTests.cs`
- `src/backend/Tests/Modules/ModuleRegistryTests.cs`
- `src/backend/Tests/Modules/IntegrationTests.cs` (activar/desactivar end-to-end)
- `src/frontend/src/__tests__/ModulesPage.spec.tsx`

---

## 🧪 Verification

- [ ] Tests unitarios del loader: discovery, ordering, error handling, dependencias cíclicas
- [ ] Tests del registry: estado en memoria, thread-safety
- [ ] Tests de integración: activar/desactivar afecta endpoints
- [ ] Tests E2E (manual): crear módulo nuevo end-to-end en <2 horas
- [ ] Tests de migración: aplicar migración de módulo, rollback si falla
- [ ] Tests de aislamiento: módulo desactivado no aparece en endpoints, no en DB queries, no en UI menu
- [ ] Performance: startup con 12 módulos activos tarda <10s
- [ ] Memory: módulos desactivados no quedan en memoria (verificado con dotnet-counters)
- [ ] Code review: el core NO tiene referencias a módulos específicos
- [ ] Documentación revisada: how-to-create-a-module.md funciona end-to-end

---

## 📝 Notas

### Decisión clave: módulos "core" vs módulos "opt-in"

No todos los módulos son opt-in. Algunos son **esenciales** (no tiene sentido desactivarlos):

| Módulo | ¿Opt-in? | Razón |
|---|---|---|
| Auth | ❌ Core | Sin auth no funciona nada |
| Sync (offline-first) | ❌ Core | Es la razón de ser del producto |
| Checkin (HU-01) | ✅ Opt-in | El gimnasio podría no usar check-in digital |
| Members (HU-02) | ✅ Opt-in | Aunque raro, podría desactivarse |
| Sales (HU-03) | ✅ Opt-in | |
| Audit (HU-06) | ✅ Opt-in | |
| Freeze (HU-07) | ✅ Opt-in | |
| Cancellation (HU-08) | ✅ Opt-in | |
| Anthropometry (HU-09) | ✅ Opt-in | |
| ProgressChart (HU-10) | ✅ Opt-in | |
| Routines (HU-11) | ✅ Opt-in | |
| Metrics (HU-12) | ✅ Opt-in | |

Esto significa que el core sigue siendo Auth + Sync (esenciales). El resto son módulos opt-in. La tabla `instance_settings` tiene una sección "core_modules_enabled" (inmutable, siempre true) y "opt_in_modules_enabled" (configurable).

### Riesgos identificados

| Riesgo | Mitigación |
|---|---|
| El refactor de las 12 HUs es masivo y rompe cosas | Hacerlo incrementalmente: primero la infra (IModule, loader, registry, tabla), después refactorizar 1 HU como piloto, después las otras 11 |
| Las migraciones de módulos viejos no son compatibles | Cada HU refactorizada debe tener su migración equivalente con `module_name` en `__EFMigrationsHistory` |
| El orden de carga de módulos importa | Definir `Dependencies` en el manifest. Si A depende de B, B se carga primero. Validar con tests. |
| El frontend no sabe qué módulos existen | El backend expone `/api/modules` con la lista, el frontend la consume. No hay hardcode de módulos en la UI. |
| Activar un módulo puede romper el schema de DB | Las migraciones son transaccionales, con rollback automático. Si fallan, el módulo queda "error" (no "active"). |
| El refactor rompe los tests existentes | Los tests de cada HU se mantienen, pero se ajustan al nuevo contexto (cada test "arranca" con su módulo activo). |

---

# Ciclo SDD (Spec-Driven Development)

> Cada fase genera un artefacto. En este archivo los consolido para mantener todo en un lugar, pero el patrón ideal es tener un archivo por fase. Para v1 los consolido.

## 1. Proposal (propuesta)

### Intent (extendido)

La visión del maintainer de GymFlow Lite es: *"si actualizo el proyecto no se borra la información del cliente, sino que ahora ya tuviese herramientas nuevas que decide si usar o no"*. Esta HU materializa esa visión.

El producto actual tiene 12 HUs implementadas como código monolítico. Esto significa que:
- El cliente no puede desactivar features que no le sirven.
- Cada nueva feature agranda el core y el footprint en memoria.
- El maintainer no puede ofrecer features premium (en un futuro open-core) sin dividir el repo.
- Terceros no pueden贡献 plugins sin tocar el core.

La solución es un **sistema de módulos opt-in**: cada feature es un módulo descubrible y activable independientemente. El cliente controla qué usa; el maintainer mantiene cada módulo aislado; terceros pueden fork-ear o (futuro) publicar plugins.

### Por qué ahora

- El proyecto acaba de cerrar su ciclo fundacional (12/12 HUs + AGPL v3 + RFC-002 + governance).
- El modelo de negocio es self-hosted ([ADR-007](docs/architecture/adr/007-modelo-self-hosted.md)) — esto REFUERZA la necesidad de opt-in (cada cliente es soberano).
- La visión "completamente gratis + si alguien lo quiere usar, que lo use" solo es sostenible si el cliente puede elegir.

### Out of scope (refinado)

- v2: DLLs externos desde `plugins/` (carga dinámica).
- v2: Hot-reload sin restart.
- v2: Marketplace / versión Pro de algunos módulos.
- v3: Sandbox de seguridad para plugins externos.

---

## 2. Spec (especificación)

### Diseño de la interfaz `IModule`

```csharp
namespace GymFlow.Core.Modules;

public interface IModule
{
    /// <summary>Identificador único estable (e.g., "sales", "routines")</summary>
    string Name { get; }

    /// <summary>Versión semántica del módulo (e.g., "1.2.0")</summary>
    string Version { get; }

    /// <summary>Nombre para mostrar en la UI</summary>
    string DisplayName { get; }

    /// <summary>Descripción para la UI</summary>
    string Description { get; }

    /// <summary>Otros módulos requeridos (se cargan antes que este)</summary>
    IReadOnlyList<string> Dependencies => Array.Empty<string>();

    /// <summary>Registra servicios en el contenedor de DI</summary>
    void RegisterServices(IServiceCollection services, IConfiguration config);

    /// <summary>Registra endpoints HTTP (controllers, minimal API, SignalR hubs)</summary>
    void RegisterEndpoints(IEndpointRouteBuilder endpoints);

    /// <summary>Jobs en background (HostedService)</summary>
    IEnumerable<IHostedService> GetBackgroundServices() => Array.Empty<IHostedService>();

    /// <summary>Migraciones de DB específicas del módulo</summary>
    IEnumerable<Migration> GetMigrations();
}
```

### Modelo de datos (instance_settings)

```sql
CREATE TABLE instance_settings (
    key VARCHAR(100) PRIMARY KEY,
    value JSONB NOT NULL,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Insertar defaults en el seed
INSERT INTO instance_settings VALUES
    ('core.version', '"1.0.0"'),
    ('core.installed_at', '"2026-06-10T00:00:00Z"'),
    ('modules.enabled_opt_in', '["checkin", "members", "sales", "audit", "freeze", "cancellation", "anthropometry", "progresschart", "routines", "metrics"]'),
    ('modules.disabled_opt_in', '[]'),
    ('modules.error', '{}');
```

### Flujo de carga (Module Loader)

```
1. App startup → ModuleLoader.Load()
2. Reflection: scan all referenced assemblies, find all IModule implementations
3. For each discovered module:
   a. Validate dependencies (all dependencies must exist)
   b. Detect cycles (topological sort)
4. Read instance_settings.modules.enabled_opt_in
5. For each enabled module:
   a. Call module.RegisterServices(services)
   b. Call module.RegisterEndpoints(endpoints)
   c. Register GetBackgroundServices() in DI
6. Build service provider
7. Run pending migrations of enabled modules (in dependency order)
8. Log: "Loaded N modules: [sales, routines, ...]"
9. If any module fails: log error, mark as "error" state, do NOT block startup
```

### API REST (admin)

```
GET    /api/admin/modules                    → List all available modules + status
POST   /api/admin/modules/{name}/enable      → Enable a module (returns 202; effective on next restart)
POST   /api/admin/modules/{name}/disable     → Disable a module
GET    /api/admin/modules/{name}/health      → Module-specific healthcheck
```

### Frontend (panel admin)

```
src/frontend/src/pages/admin/ModulesPage.tsx
- Lista de módulos en cards
- Cada card: DisplayName, Description, Version, Status (Active/Inactive/Error), toggle
- Botón "Save & Restart" → activa el cambio + sugiere restart
- Logs visibles por módulo (opcional)
```

---

## 3. Design (diseño técnico)

### Estructura de carpetas (post-refactor)

```
src/backend/
├── GymFlow.Core/                          ← Infrastructure + abstractions
│   ├── Modules/
│   │   ├── IModule.cs
│   │   ├── ModuleManifest.cs
│   │   ├── ModuleContext.cs
│   │   ├── ModuleLoader.cs
│   │   ├── ModuleRegistry.cs
│   │   └── ModuleState.cs
│   ├── GymFlowDbContext.cs
│   └── ...
├── GymFlow.Application/                   ← UseCases
│   ├── Modules/
│   │   ├── GetAvailableModulesUseCase.cs
│   │   ├── EnableModuleUseCase.cs
│   │   └── DisableModuleUseCase.cs
│   └── ...
├── GymFlow.Infrastructure/                ← Implementations
│   ├── Modules/
│   │   ├── ModuleSettingsStore.cs
│   │   └── Migrations/
│   │       └── (migraciones por módulo)
│   ├── Persistence/
│   └── ...
├── GymFlow.WebAPI/                        ← HTTP
│   ├── Controllers/Admin/ModulesController.cs
│   └── ...
└── Modules/                                ← LOS MÓDULOS (carpeta nueva)
    ├── GymFlow.Module.Sales/
    │   ├── SalesModule.cs                  ← implementa IModule
    │   ├── Domain/                         ← entidades específicas
    │   ├── Application/                    ← use cases específicos
    │   ├── Infrastructure/                 ← repos específicos
    │   ├── Migrations/                     ← migraciones específicas
    │   └── GymFlow.Module.Sales.csproj
    ├── GymFlow.Module.Routines/
    │   └── ...
    ├── GymFlow.Module.Metrics/
    │   └── ...
    └── (10 módulos más)
```

### Convenciones de naming

- Cada módulo es un **.csproj separado** (assembly independiente).
- Namespace: `GymFlow.Module.{Name}` (e.g., `GymFlow.Module.Sales`).
- Carpeta: `src/backend/Modules/GymFlow.Module.{Name}/`.
- Migraciones: `src/backend/Modules/GymFlow.Module.{Name}/Migrations/`.

### Inyección de dependencias

- El **core** registra `IModuleLoader`, `IModuleRegistry`, `IModuleSettingsStore` en el startup.
- El **loader** descubre los módulos vía `AppDomain.CurrentDomain.GetAssemblies()` + reflection.
- Cada **módulo** registra sus servicios en `RegisterServices(services, config)`.
- El **registry** mantiene `Dictionary<string, ModuleState>` en memoria (singleton).

### Manejo de errores

| Escenario | Comportamiento |
|---|---|
| Módulo no se puede cargar (assembly error) | Log error, módulo en estado "error", resto sigue |
| Dependencia faltante | Log error, módulo en estado "error", resto sigue |
| Ciclo de dependencias | Log error al startup, módulo en estado "error", resto sigue |
| Migración falla | Rollback automático, módulo en estado "error", DB intacta |
| Endpoint tira exception en runtime | Error 500 standard, módulo sigue activo, logs |

### Performance

- Discovery: 1 vez al startup, < 100ms con 12 módulos.
- Activación/desactivación: requiere restart (v1). Startup con 12 módulos: < 5s.
- Memoria: módulos desactivados NO se cargan (ahorra RAM).

---

## 4. Tasks (desglose de implementación)

### Batch A — Infraestructura del sistema de módulos (1 sprint, 2-3 semanas)

1. **A1** — Crear `IModule` y contratos (1 día)
2. **A2** — Implementar `ModuleLoader` con reflection-based discovery (2-3 días)
3. **A3** — Implementar `ModuleRegistry` con state en memoria (1 día)
4. **A4** — Crear tabla `instance_settings` + migración (1 día)
5. **A5** — Implementar `ModuleSettingsStore` (CRUD en JSONB) (2 días)
6. **A6** — Implementar `ModulesController` con API admin (1-2 días)
7. **A7** — Tests unitarios del loader, registry, settings store (2-3 días)
8. **A8** — Tests de integración end-to-end (activar/desactivar un módulo ficticio) (2-3 días)
9. **A9** — Seed: insertar defaults en `instance_settings` (medio día)
10. **A10** — Logging estructurado del loader (1 día)

**Criterio de fin del Batch A**: el sistema de módulos existe, funciona con un módulo de prueba "Hello World", tiene tests pasando, y la API admin responde.

### Batch B — Refactor de las 12 HUs en módulos (2-3 sprints, 4-6 semanas)

Para cada HU existente (HU-01 a HU-12), en este orden de prioridad:

1. **B1** — Identificar qué es "core" vs qué es opt-in
2. **B2** — Crear el csproj del módulo
3. **B3** — Mover Domain, Application, Infrastructure específicas al módulo
4. **B4** — Crear la clase `XxxModule : IModule` con los hooks
5. **B5** — Mover las migraciones de DB al módulo (renombrar si es necesario)
6. **B6** — Ajustar tests para que asuman el módulo activo
7. **B7** — Ajustar la doc técnica de la HU (mencionar que ahora es módulo opt-in)

**Orden sugerido** (de menos a más complejo):
- B-Sales (HU-03) — primer piloto, ya tiene 10 CAs
- B-Checkin (HU-01) — crítico, no se puede desactivar
- B-Members (HU-02) — fundamental
- B-Audit (HU-06) — independiente
- B-Freeze (HU-07) — depende de Members
- B-Cancellation (HU-08) — depende de Members
- B-Anthropometry (HU-09) — depende de Members
- B-ProgressChart (HU-10) — depende de Anthropometry
- B-Routines (HU-11) — independiente
- B-Metrics (HU-12) — depende de Sales + Members

**Criterio de fin del Batch B**: las 12 HUs son módulos opt-in. El admin puede activar/desactivar cada uno.

### Batch C — UI del panel de módulos (1 sprint, 1-2 semanas)

1. **C1** — Crear `ModulesPage` con lista de módulos en cards (2 días)
2. **C2** — Crear `ModuleCard` con toggle + estado (1 día)
3. **C3** — Crear `moduleService` (frontend) (medio día)
4. **C4** — Integrar con `dashboardService` y `useEnabledModules` hook (1 día)
5. **C5** — Mostrar/ocultar items del menú según módulos activos (1-2 días)
6. **C6** — Tests E2E del panel (1-2 días)

**Criterio de fin del Batch C**: el Owner puede ver la lista de módulos, activar/desactivar, y los cambios se reflejan en el menú.

### Batch D — Documentación (1-2 días, paralelo a todo lo anterior)

1. **D1** — Crear `docs/technical/how-to-create-a-module.md` (1 día)
2. **D2** — Actualizar README.md con mención al sistema de módulos (medio día)
3. **D3** — Actualizar `docs/architecture/adr/008-arquitectura-de-modulos.md` si hay desvíos (continuo)

**Criterio de fin del Batch D**: un dev nuevo puede crear un módulo siguiendo la doc.

### Verificación final (HU-015 completa cuando...)

- [ ] Las 12 HUs son módulos opt-in funcionales
- [ ] El panel de admin permite activar/desactivar
- [ ] La API admin responde correctamente
- [ ] Los tests pasan (unit + integration)
- [ ] La documentación "how-to-create-a-module" funciona end-to-end
- [ ] El core NO tiene referencias a módulos específicos
- [ ] La performance es aceptable (startup < 10s con 12 módulos)
- [ ] El refactor no rompió HU-13 (CI/CD sigue verde)

---

## 🔗 Referencias

- [ADR-008: Sistema de módulos opt-in](docs/architecture/adr/008-arquitectura-de-modulos.md) — la decisión
- [RFC-002 §6 Horizonte 1](docs/architecture/rfc/002-modelo-de-negocio-y-gobernanza.md#6-roadmap-estratégico) — el lugar en el roadmap
- [HU-017 Schema versioning](docs/tasks/HU-001-HU-099/HU-017-schema-versioning.md) — feature paralela
- [HU-016 CLI + installer](docs/tasks/HU-001-HU-099/HU-016-cli-installer.md) — feature que consume módulos
- [ADR-001 Stack](docs/architecture/adr/001-stack-tecnologico.md) — Clean Architecture donde encajan los módulos
- [ADR-003 Migraciones](docs/architecture/adr/003-estrategia-migraciones.md) — base para migraciones de módulos
- Odoo Apps — precedente
- [template-hu-sdd.md](docs/templates/hu/template-hu-sdd.md) — template usado
