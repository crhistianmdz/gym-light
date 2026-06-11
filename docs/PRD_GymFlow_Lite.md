# Product Requirement Document (PRD) — GymFlow Lite

> **Documento vivo**. Define el QUÉ del producto (reglas de negocio, alcance, target).
> El CÓMO vive en los ADRs (`docs/architecture/adr/`). El modelo de distribución, monetización y gobernanza vive en RFC-002 (`docs/architecture/rfc/002-modelo-de-negocio-y-gobernanza.md`).
>
> **Versión**: 2.0 — actualizada 2026-06-10 (alineada con RFC-002 y ADR-006/007).

---

## 1. Visión y Objetivo

GymFlow Lite es una **plataforma de gestión open-source AGPL v3** para **gimnasios pequeños (10-200 miembros)**, distribuida como **self-hosted** (cada cliente corre su propia instancia). El sistema garantiza **continuidad operativa total** mediante una arquitectura **Offline-First**, permitiendo control de acceso y ventas sin interrupciones, independientemente de la estabilidad de la conexión a internet.

### Objetivo dual del proyecto

| Objetivo | Para quién | Por qué |
|---|---|---|
| **Probar tecnologías** con un vehículo real | El maintainer (primario) | El proyecto es un laboratorio personal de aprendizaje, no un producto comercial. Las features se eligen también en función de qué tecnologías queremos explorar. |
| **Tener un producto profesional** que terceros puedan usar, instalar, fork-ear o donar | La comunidad (secundario) | Si el resultado es útil para alguien más, mejor. Pero el "éxito" se mide en valor entregado, no en métricas de growth agresivo. |

**Ambos objetivos son válidos y no compiten.** Un proyecto que prueba tecnologías y resulta profesional es mejor que uno que solo hace una cosa bien. Si entran en conflicto, el objetivo primario (personal) gana.

### Lo que GymFlow Lite NO es

- **NO es un SaaS** que el maintainer opera. Ver [ADR-007](docs/architecture/adr/007-modelo-self-hosted.md).
- **NO es open-core** con features premium pagas en código cerrado. Todo es AGPL v3.
- **NO es un proyecto comercial** con SLAs, soporte 24/7, ni on-call rotation.
- **NO es "WordPress para gimnasios"** (esa frase ha sido usada por la competencia; GymFlow Lite tiene su propia personalidad).

---

## 2. Distribución y Licencia

| Aspecto | Decisión | Doc de referencia |
|---|---|---|
| **Modelo de distribución** | Self-hosted (cada cliente corre su propia instancia) | [ADR-007](docs/architecture/adr/007-modelo-self-hosted.md) |
| **Licencia** | GNU Affero General Public License v3.0 (AGPL v3) | [ADR-006](docs/architecture/adr/006-licencia-agpl-v3.md) |
| **Distribución primaria** | GitHub (código fuente) + Docker Compose (instalación recomendada) | [RFC-002 §3](docs/architecture/rfc/002-modelo-de-negocio-y-gobernanza.md#3-distribución) |
| **Monetización** | Oportunista: donaciones (GitHub Sponsors, OpenCollective) + servicios profesionales opcionales + soporte pago si lo piden | [RFC-002 §4](docs/architecture/rfc/002-modelo-de-negocio-y-gobernanza.md#4-monetización-oportunista-no-agresiva) |
| **Gobernanza** | 1 maintainer, contributors externos bienvenidos, code of conduct aplicado | [RFC-002 §5](docs/architecture/rfc/002-modelo-de-negocio-y-gobernanza.md#5-gobernanza) |

---

## 3. Roles y Permisos (RBAC)

| Rol | Responsabilidad Clave |
| :--- | :--- |
| **Owner** | Visualización de KPIs financieros, Churn Rate y gestión multi-sede. Único rol con visibilidad de métricas de negocio. |
| **Admin** | Configuración de planes, auditoría de logs y validación de pagos. Puede ver métricas también. |
| **Receptionist** | Operación diaria: Check-in, registro de socios y ventas POS. NO ve métricas. |
| **Trainer** | Creación de rutinas digitales y seguimiento antropométrico. |
| **Member** | Consulta de progreso y check-list de rutinas. |

---

## 4. Alcance del Proyecto por Fases

### Fase 1: MVP & Operación Core ✅ COMPLETA (12/12 HUs implementadas)

- ✅ **Gestión de Usuarios** (HU-02): Registro con captura de foto obligatoria (WebP) para validación de identidad.
- ✅ **Control de Acceso Offline** (HU-01): Validación contra caché local (IndexedDB) en <200ms.
- ✅ **Suscripciones y Pagos** (HU-12): Ciclo de vida de membresías (Activa/Vencida) con registro de confirmación externa. Los cobros se registran como `Payment` (separado de `Sale`). Cada `Payment` tiene una categoría (`Membership` o `POS`).
- ✅ **Punto de Venta (POS)** (HU-03): Venta de productos con alerta de stock crítico al alcanzar el 20%.
- ✅ **Sincronización** (HU-04): Motor de sincronización idempotente para subir logs locales al detectar red.
- ✅ **Auditoría Básica** (HU-06): Registro de "quién hizo qué" (Logs de transacciones).
- ✅ **HU-12 — Dashboard de Métricas**: Dashboard para Owner/Admin con reporte de ingresos mensuales por categoría y tasa de churn.

### Fase 2: Fidelización y Gestión Avanzada ✅ COMPLETA (incluida en 12/12)

- ✅ **Módulo de Congelamiento** (HU-07) con reglas estrictas: máx 4 eventos/año, mín 7 días/evento, bloqueo inmediato, extensión automática del `EndDate`.
- ✅ **Política de Cancelación** (HU-08): Acceso Residual, no hay reembolsos automáticos.
- ✅ **Seguimiento de Salud** (HU-09): Medidas antropométricas con soporte métrico/imperial, offline-first.
- ✅ **Gráficas de Evolución** (HU-10): Visualización del progreso físico.
- ✅ **Rutinas Digitales** (HU-11): Constructor de rutinas y seguimiento.

### Fase 3: Distribución y Ecosistema ⏳ EN PROGRESO (ver RFC-002)

- ⏳ **Sistema de plugins opt-in** (HU-015 planeada)
- ⏳ **CLI de GymFlow** (HU-016 planeada)
- ⏳ **Schema versioning + migraciones aditivas** (HU-017 planeada)
- ⏳ Demo online público
- ⏳ Documentación de instalación y upgrade
- ⏳ GitHub Sponsors + OpenCollective activados

Ver [RFC-002 §6](docs/architecture/rfc/002-modelo-de-negocio-y-gobernanza.md#6-roadmap-estratégico) para el roadmap completo con criterios de éxito.

---

## 5. Reglas de Negocio Consolidadas

1. **Validación de Acceso** (HU-01): No se permite el ingreso si la suscripción está vencida o congelada.
2. **Seguridad de Identidad** (HU-02): La foto de perfil es un requisito técnico para habilitar el check-in.
3. **Prioridad de Datos** (RFC-001): En caso de conflicto entre local y nube, prevalece la **Autoridad del Servidor**.
4. **Idempotencia** (RFC-001): Cada transacción local genera un UUID (ClientGuid) único para evitar duplicados.
5. **Inventario** (HU-03): El sistema debe impedir ventas offline si el stock local registrado es 0.
6. **Offline-First** (RFC-001): El sistema DEBE poder operar offline por tiempo indefinido para check-in y ventas.
7. **Licencia AGPL v3** ([ADR-006](docs/architecture/adr/006-licencia-agpl-v3.md)): Todo el código贡献 al proyecto se licencia bajo AGPL v3.

---

## 6. Especificaciones Técnicas (Stack)

| Capa | Tecnología | ADR |
|---|---|---|
| **Backend** | .NET 8 (Web API) con Clean Architecture | [ADR-001](docs/architecture/adr/001-stack-tecnologico.md) |
| **Frontend** | React (PWA) con Service Workers | [ADR-001](docs/architecture/adr/001-stack-tecnologico.md) |
| **Persistencia cloud** | PostgreSQL + Entity Framework Core (Npgsql) | [ADR-003](docs/architecture/adr/003-estrategia-migraciones.md) |
| **Persistencia local** | IndexedDB (vía Dexie.js) | [ADR-001](docs/architecture/adr/001-stack-tecnologico.md) |
| **Cache server-side** | Redis | [ADR-001](docs/architecture/adr/001-stack-tecnologico.md) |
| **Autenticación** | JWT (in-memory) + Refresh Tokens en HttpOnly Cookies | [ADR-002](docs/architecture/adr/002-estrategia-autenticacion.md) |
| **UI Kit** | Material Design (MUI) | [ADR-001](docs/architecture/adr/001-stack-tecnologico.md) |
| **Infra local** | Docker Compose | [ADR-001](docs/architecture/adr/001-stack-tecnologico.md) |
| **CI/CD** | GitHub Actions (ci.yml + release-please + security-review) | [docs/technical/hu13-cicd.md](docs/technical/hu13-cicd.md) |

---

## 7. Requerimientos No Funcionales

| Requerimiento | Métrica objetivo | Cómo se mide |
|---|---|---|
| **Disponibilidad offline** | El sistema debe poder operar offline para check-in y ventas por **tiempo indefinido** | No requiere medición (es por diseño, RFC-001) |
| **Latencia de interfaz** | Latencia en recepción **< 200ms** | Manual / métricas opcionales |
| **Observabilidad UX** | Indicador visual de estado de sincronización (Sincronizado/Pendiente/Offline) | Implementado en HU-01, HU-04 |
| **Idempotencia** | Cualquier operación local puede ser sincronizada N veces sin duplicar efectos | Garantizado por ClientGuid (RFC-001, todas las HUs) |
| **Privacidad de datos del cliente** | Cada cliente es dueño absoluto de su data (self-hosted) | Por diseño del modelo de distribución ([ADR-007](docs/architecture/adr/007-modelo-self-hosted.md)) |
| **Resiliencia ante pérdida de conexión** | La operación diaria (check-in, ventas) nunca se interrumpe por falta de internet | Offline-first ([RFC-001](docs/RFC_001_Architecture_Offline_Sync.md)) |
| **Mantenibilidad por 1 persona** | El maintainer (1 dev senior) puede mantener el proyecto a tiempo parcial | Por diseño del modelo (sin DevOps, sin on-call) |

---

## 8. Status actual del proyecto (a 2026-06-10)

- ✅ **12/12 HUs del backlog original implementadas y commiteadas** (HU-01 a HU-12).
- ✅ **HU-13 (CI/CD) cerrada** con 3 workflows + docs técnica.
- ✅ **5 ADRs técnicos fundacionales** (001-005).
- ✅ **2 ADRs estratégicos nuevos** (006 licencia, 007 self-hosted).
- ✅ **RFC-002 aprobada** (modelo de negocio y gobernanza).
- ✅ **LICENSE file con AGPL v3** completo.
- ✅ **13 docs técnicas de HU** + 8 docs técnicas generales.
- ✅ **FlowDocs framework** (12 templates) implementado.
- ⏳ **Fase 3 (distribución y ecosistema)** en planificación — ver [RFC-002 §6](docs/architecture/rfc/002-modelo-de-negocio-y-gobernanza.md#6-roadmap-estratégico).

**Métrica principal de éxito**: el proyecto sigue siendo **divertido de mantener** + **útil para alguien**. Las métricas de stars/downloads/donaciones son **oportunistas**, no objetivo.

---

## 9. Referencias cruzadas

| Si querés entender... | Leé... |
|---|---|
| **Por qué AGPL v3** | [ADR-006](docs/architecture/adr/006-licencia-agpl-v3.md) |
| **Por qué self-hosted (no SaaS)** | [ADR-007](docs/architecture/adr/007-modelo-self-hosted.md) |
| **El "contrato social" del proyecto** | [RFC-002](docs/architecture/rfc/002-modelo-de-negocio-y-gobernanza.md) |
| **Cómo se sincroniza offline** | [RFC-001](docs/RFC_001_Architecture_Offline_Sync.md) |
| **El stack técnico** | [ADR-001](docs/architecture/adr/001-stack-tecnologico.md) |
| **Cómo se auth** | [ADR-002](docs/architecture/adr/002-estrategia-autenticacion.md) |
| **Cómo se migra la DB** | [ADR-003](docs/architecture/adr/003-estrategia-migraciones.md) |
| **Las HUs del backlog** | [User_Stories_GymFlow.md](docs/tasks/User_Stories_GymFlow.md) |
| **El estado de implementación** | [implementation-status.md](docs/technical/implementation-status.md) |
| **El changelog** | [CHANGELOG.md](CHANGELOG.md) |
| **Cómo contribuir (reglas operacionales)** | [AGENTS.md](AGENTS.md) |
| **La licencia** | [LICENSE](LICENSE) |

---

**Mantenimiento del PRD**: este documento se actualiza cuando cambia el alcance, el target, las reglas de negocio, o las prioridades estratégicas. **NO** se actualiza por cada feature nueva (eso vive en la HU correspondiente). Decisiones técnicas (POR QUÉ elegimos X) van en ADRs. Decisiones estratégicas (modelo de negocio, distribución) van en RFCs.
