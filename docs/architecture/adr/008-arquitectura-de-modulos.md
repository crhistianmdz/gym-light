# ADR-008: Sistema de Módulos / Plugins Opt-In

**Status**: 🟢 Accepted
**Date**: 2026-06-10
**Deciders**: @gymflow-tech-lead

---

## 🎯 Contexto

GymFlow Lite es self-hosted ([ADR-007](007-modelo-self-hosted.md)), open-source AGPL v3 ([ADR-006](006-licencia-agpl-v3.md)), y busca ser un producto que terceros puedan **usar, instalar, fork-ear o donar** ([RFC-002](002-modelo-de-negocio-y-gobernanza.md)). La visión del maintainer incluye explícitamente: *"si actualizo el proyecto no se borra la información del cliente, sino que ahora ya tuviese herramientas nuevas que decide si usar o no"*.

Esto requiere que el producto:

1. **Reciba features nuevas** en cada release, sin requerir que el cliente instale una nueva versión distinta del producto.
2. **Permita al cliente activar/desactivar features** individualmente, sin tocar código.
3. **Soporte plugins de terceros** (futuro) sin que el maintainer tenga que mantener cada uno.
4. **Mantenga un core estable** que los upgrades no rompan, con migraciones aditivas ([HU-017](docs/tasks/HU-001-HU-099/HU-017-schema-versioning.md)).

El patrón técnico que materializa esta visión se llama **"sistema de módulos / plugins opt-in"** (también conocido como "modular monolith" o "plugin architecture"). Es lo que usan Odoo (módulos), WordPress (plugins), Discourse (plugins), VSCode (extensions), Grafana (plugins), etc.

### Fuerzas en juego

| Fuerza | Detalle |
|---|---|
| **Visión del maintainer** | "El cliente decide si usa o no" → opt-in es explícito. |
| **Modelo self-hosted** | Cada cliente corre SU instancia. Si hay features que no quiere, no deberían estar ahí. |
| **Múltiples roles** | Receptionist, Trainer, Admin, Owner, Member — cada rol puede tener módulos activos distintos. |
| **Plugins de terceros (futuro)** | Si terceros quieren agregar funcionalidad, deben poder hacerlo sin tocar el core. |
| **Stack .NET 8** | Tiene soporte nativo para DI, reflection, y carga de assemblies. Buen fit para discovery automático. |
| **Tamaño del equipo** | 1 dev senior. El sistema debe ser simple de mantener, no sobre-ingenieril. |
| **Compatibilidad con Clean Architecture** | El core actual usa Domain/Application/Infrastructure/WebAPI. Los módulos deben encajar sin romper las capas. |
| **Riesgo de over-engineering** | Sistema de plugins es la cosa más facil de hacer MAL. Tiene que ser simple, claro, y debuggeable. |

### Restricciones

- El sistema debe ser **opt-in REAL**: si el módulo no está activado, ni se carga en memoria.
- El core del producto **no debe depender** de ningún módulo específico (inversión de dependencias).
- La activación/desactivación debe ser **persistente** (sobrevive a reinicios) y **atómica** (no se puede dejar en estado inconsistente).
- Los módulos deben poder **tener sus propias migraciones de DB** (HU-017) sin romper el core.
- Los módulos deben poder **registrar endpoints HTTP, servicios, jobs, etc.** de forma estandarizada.
- La carga/descarga de un módulo **no debe requerir reinicio** del servidor (idealmente).

---

## 🤔 Decisión

**Elegimos**: **Modular Monolith con Auto-Descubrimiento + Activación Persistente + Carga de Assemblies Externos**.

El sistema se compone de:

1. **Interfaz `IModule`** que cada módulo implementa (en el core o en assemblies externos).
2. **Descubrimiento automático** de módulos vía reflection sobre assemblies referenciados (core) y/o carga dinámica desde una carpeta `plugins/` (terceros).
3. **Tabla `instance_settings`** en la DB con un JSON que lista los módulos activos.
4. **Module Loader** en el startup que: (a) descubre, (b) carga, (c) registra servicios/endpoints/migrations, (d) respeta el flag `enabled`.
5. **Sin reinicio para activar/desactivar** vía signal/event (futuro, no en v1).

El core **NO contiene ningún módulo específico del producto** (Sales, Routines, Metrics, etc.). Los módulos existentes (los HUs 01-12) se **refactorizan** en módulos opt-in: cada uno cumple `IModule`, se auto-registra, y se activa por default en instancias nuevas (para mantener compatibilidad con la instalación actual).

**Resultado esperado**: el cliente puede ir a su panel de admin, ver la lista de módulos disponibles, activar/desactivar los que quiera, y la próxima vez que el server reinicie (o el módulo reciba la señal) los cambios se aplican.

### Arquitectura de alto nivel

```
┌─────────────────────────────────────────────────────────────────┐
│                         Core (GymFlow.Core)                      │
│  ┌─────────────────┐  ┌──────────────────┐  ┌────────────────┐ │
│  │   IModule       │  │ ModuleLoader     │  │ ModuleRegistry │ │
│  │   (interface)   │  │ (lifecycle)      │  │ (state)        │ │
│  └─────────────────┘  └──────────────────┘  └────────────────┘ │
│                                                                 │
│  ┌─────────────────┐  ┌──────────────────┐  ┌────────────────┐ │
│  │   IModuleStore  │  │ ModuleManifest   │  │ ModuleContext  │ │
│  │   (DB CRUD)     │  │ (metadata)       │  │ (per-request)  │ │
│  └─────────────────┘  └──────────────────┘  └────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
                              ↑ implementa
            ┌─────────────────┼─────────────────┐
            │                 │                 │
    ┌───────▼──────┐  ┌───────▼──────┐  ┌───────▼──────┐
    │ Module.Sales │  │ Module.     │  │ Module.     │
    │ (HU-03)      │  │ Routines    │  │ Metrics     │
    │              │  │ (HU-11)     │  │ (HU-12)     │
    └──────────────┘  └──────────────┘  └──────────────┘
            │                 │                 │
            └─────────────────┼─────────────────┘
                              ↓ carga opcional
                    ┌──────────────────┐
                    │  plugins/        │
                    │  (third-party)   │
                    │  *.dll externos  │
                    └──────────────────┘
```

---

## ⚖️ Opciones Consideradas

### Opción 1: Sin sistema de módulos (monolito plano, status quo)

**Descripción**: El código sigue como está. Las features se agregan al core sin separación. No hay opt-in.

**Pros**:
- Cero cambio al código actual.
- Simple de entender.
- Cero overhead de discovery / reflection.

**Contras**:
- **NO cumple la visión** del maintainer ("el cliente decide si usa o no").
- **NO escala** hacia plugins de terceros.
- El cliente no puede desactivar features que no le sirven.
- Cada nueva feature agranda el core y el footprint en memoria.
- Tests se vuelven más difíciles (no se puede testear módulos aislados).

### Opción 2: Microservicios (cada módulo es un servicio independiente) ❌

**Descripción**: Cada HU es un servicio independiente que se comunica vía HTTP/gRPC/Kafka.

**Pros**:
- Aislamiento total.
- Deploy independiente.
- Escala por servicio.

**Contras**:
- **Complejidad operacional enorme** para un proyecto self-hosted con 1 dev.
- Requiere Kubernetes o similar.
- Latencia inter-servicio.
- Debug distribuido.
- **Incompatible con el modelo self-hosted** del proyecto (cliente tendría que operar 15+ servicios).

### Opción 3: Modular Monolith con auto-descubrimiento (reflection) ✅ (elegida)

**Descripción**: Todo en un solo proceso, pero el código se organiza en módulos. Reflection encuentra los `IModule` y los registra. Activación vía flag en DB.

**Pros**:
- **Un solo proceso, una sola DB, un solo deploy** — simple operacionalmente.
- El cliente tiene control sobre qué módulos corren.
- Cero overhead en runtime (los módulos no activados no se cargan).
- Reflection es nativo en .NET, no requiere libs externas.
- Encaja con Clean Architecture actual (Domain/Application/Infrastructure/WebAPI).
- Es lo que hace Odoo, Discourse, etc. — **precedente sólido**.

**Contras**:
- Los módulos comparten proceso → un módulo buggy puede tumbar el core.
- Si crece mucho (>50 módulos), el startup puede ser lento.
- Tests E2E más complejos (hay que probar con/sin módulos).
- Requiere disciplina: los módulos no deben "romper" las capas de Clean Architecture.

### Opción 4: Plugin dinámico vía carga de DLLs externos

**Descripción**: El core descubre DLLs en una carpeta `plugins/` y los carga en runtime.

**Pros**:
- Permite **plugins de terceros** sin recompilar el core.
- Cero acoplamiento con assemblies específicos.
- Mercado de plugins posible.

**Contras**:
- **Complejidad de deployment**: el cliente tiene que gestionar la carpeta `plugins/`.
- Versionado de interfaces: si el core cambia, los plugins de terceros pueden romperse.
- Seguridad: cargar DLLs externos tiene riesgos (validar firma, sandboxing, etc.).
- Para v1, **no es necesario**: el maintainer controla todos los módulos.

**Decisión**: lo dejamos como **futuro** (v2 o v3 del sistema de módulos). Para v1, auto-descubrimiento es suficiente. Terceros pueden fork-ear el repo y贡献 módulos al core mientras tanto.

### Opción 5: Sidecar pattern (módulos como procesos separados pero acoplados)

**Descripción**: Cada módulo corre como un proceso separado, pero se comunica con el core vía IPC/local socket.

**Pros**:
- Mejor aislamiento que monolith.
- Más simple que microservicios.

**Contras**:
- Mismo overhead operacional que microservicios (múltiples procesos).
- Debug complejo.
- **No aporta** sobre el modular monolith para nuestro caso.

### Opción 6: PaaS-style (Heroku-like, módulos como "add-ons")

**Descripción**: Marketplace de add-ons, cada uno con su propio deploy, conectado al core vía API.

**Pros**:
- Ecosistema abierto.

**Contras**:
- Requiere infraestructura central (Heroku, etc.).
- **Incompatible con self-hosted**.

---

## 📐 Consecuencias

### Positivas

- **La visión se materializa**: el cliente decide qué features activas.
- **Cero overhead en runtime**: módulos no activados no se cargan (ahorra memoria y superficie de ataque).
- **Compatible con Clean Architecture** actual: los módulos pueden respetar las capas o ser cross-cutting.
- **Habilita plugins de terceros** (v2): un tercero puede forkear, agregar un módulo, y贡献lo.
- **Testeable aisladamente**: cada módulo se puede probar sin el resto.
- **El core permanece pequeño y mantenible**: cada HU es un módulo separado, no un patch al core.
- **El upgrade es seguro** ([HU-017](docs/tasks/HU-001-HU-099/HU-017-schema-versioning.md)): los módulos tienen sus propias migrations, no tocan el core.
- **Precedente exitoso**: Odoo (módulos), Discourse (plugins), WordPress (plugins), VSCode (extensions), Grafana (plugins).

### Negativas

- **Esfuerzo de refactor significativo**: las 12 HUs actuales se convierten en 12 módulos. Esto es trabajo, no es trivial.
- **Riesgo de "feature flag explosion"**: si cada feature se separa en módulo, hay muchos módulos. Hay que encontrar el balance.
- **Disciplina arquitectónica requerida**: los módulos no deben depender entre sí (acoplamiento). Hay que establecer convenciones.
- **El "core" debe ser genuinamente mínimo**: si no, los módulos terminan dependiendo de cosas del core que no deberían.
- **Testing más complejo**: hay que probar el "core solo", el "core + módulo A", el "core + todos los módulos".
- **Documentación adicional requerida**: cómo se crea un módulo, qué APIs están disponibles, etc.
- **Si se hace mal, es un infierno de debug**: el loader debe ser claro, los logs detallados, los errores específicos.

### Neutras

- **El "core" se reduce** a: autenticación, autorizaciones, módulo loader, DB abstraction, frontend shell.
- **Los módulos existentes se refactorizan** (HU-01 a HU-12 → módulos). Esto es trabajo de 1-2 sprints adicionales.
- **El plugin de terceros (DLLs externos) queda como v2**, no se implementa en este ADR.
- **La activación/desactivación requiere restart** en v1 (signal-based hot-reload queda como v2).
- **El panel de admin para gestionar módulos** es parte de la implementación (no solo backend).

---

## 🔗 Referencias

- **Odoo Apps** (módulos): <https://www.odoo.com/apps> — precedente más cercano
- **WordPress Plugin Handbook**: <https://developer.wordpress.org/plugins/>
- **Discourse Plugin Architecture**: <https://meta.discourse.org/t/plugin-architecture-overview/29750>
- **Microsoft.Extensions.Hosting + DI**: <https://learn.microsoft.com/en-us/dotnet/core/extensions/hosting>
- **Reflection in .NET**: <https://learn.microsoft.com/en-us/dotnet/fundamentals/reflection/reflection>
- [ADR-001](001-stack-tecnologico.md) — Stack (Clean Architecture es el fit)
- [ADR-006](006-licencia-agpl-v3.md) — Licencia (los plugins de terceros también son AGPL v3)
- [ADR-007](007-modelo-self-hosted.md) — Distribución
- [RFC-002 §6](002-modelo-de-negocio-y-gobernanza.md#6-roadmap-estratégico) — Roadmap (HU-015, HU-016, HU-017)
- [HU-015](docs/tasks/HU-001-HU-099/HU-015-sistema-de-modulos.md) — Implementación concreta
- [HU-017](docs/tasks/HU-001-HU-099/HU-017-schema-versioning.md) — Schema versioning
- [HU-016](docs/tasks/HU-001-HU-099/HU-016-cli-installer.md) — CLI que aprovecha los módulos
- [template-hu-sdd.md](docs/templates/hu/template-hu-sdd.md) — Template para HU grandes
