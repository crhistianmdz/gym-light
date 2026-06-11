# RFC-002: Modelo de Negocio y Gobernanza de GymFlow Lite

**Status**: 🟢 Accepted
**Author**: @gymflow-tech-lead
**Created**: 2026-06-10
**Reviewers**: @gymflow-contributors
**Supersedes**: ninguna (es la primera RFC de gobernanza)

---

## 🎯 Resumen

Esta RFC define **cómo se distribuye, mantiene, monetiza y gobierna GymFlow Lite** como proyecto open-source. La decisión fundacional es: GymFlow Lite es un **producto self-hosted, open-source AGPL v3, completamente gratis para el usuario final**, con un modelo de monetización **oportunista** basado en donaciones, servicios profesionales opcionales y soporte pago. El objetivo dual del proyecto es **probar tecnologías** (objetivo personal del maintainer) y **tener un producto profesional** que terceros puedan usar, instalar, fork-ear o donar si lo valoran.

Esta RFC **no introduce código nuevo**. Es un documento de gobernanza que define las reglas del juego, los compromisos del maintainer hacia la comunidad, y el roadmap estratégico. Una vez aceptada, todas las decisiones técnicas futuras deben ser coherentes con ella.

---

## 🎯 Motivación

GymFlow Lite nació como un proyecto personal con foco en resolver un problema real (gestión de gimnasios pequeños) y aprender tecnologías en el camino. Después de 12 HUs implementadas y 5 ADRs técnicos aprobados, el proyecto tiene un **producto core funcionando** pero **carece de gobernanza explícita**:

- No hay `LICENSE` file (no se puede decir legalmente "es open-source X").
- El modelo de distribución no está documentado (¿es SaaS? ¿es self-hosted?).
- No hay reglas claras sobre cómo贡献, qué se acepta, qué se rechaza.
- El roadmap estratégico no existe como tal — los commits cuentan la historia pero no el plan.
- La "marca" (GymFlow Lite) no está protegida ni documentada.

Esta RFC llena esos vacíos. Es fundacional: cualquier ADR, RFC o HU futuro debe ser coherente con lo que aquí se decida.

### Contexto personal del maintainer

Esta RFC es explícita sobre algo que normalmente se omite: **el maintainer es una persona, no una empresa**. Eso significa:

- Tiene un trabajo principal (no vive de GymFlow).
- Tiene tiempo limitado para贡献.
- Tiene costos personales (servidor para el demo, dominio, etc.) que NO son cubiertos por el proyecto.
- Le interesa el problema del gimnasio, pero más le interesa **aprender tecnologías nuevas**.
- El "éxito" del proyecto se mide en **valor entregado a la comunidad + aprendizaje personal**, no en MRR ni en # de instalaciones.

Esto es **información relevante** para contribuidores y usuarios, porque define expectativas realistas sobre la cadencia de releases, la velocidad de respuesta a issues, y la longevidad del proyecto.

---

## 📋 Propuesta Detallada

### 1. Visión y objetivos

#### 1.1. Objetivo dual (ambos son legítimos y válidos)

| Objetivo | Para quién | Métrica de éxito |
|---|---|---|
| **Probar tecnologías** | El maintainer (principal) | Cantidad y diversidad de tecnologías probadas, calidad del código resultante, satisfacción personal |
| **Producto profesional que otros usen** | La comunidad (secundario) | Stars, forks, instalaciones reportadas, contribuidores activos, donaciones recibidas |

**Ambos objetivos son válidos y no compiten.** Un proyecto que prueba tecnologías y resulta profesional es mejor que uno que solo hace una cosa bien. El orden importa: el objetivo primario es el personal; el secundario es el comunitario. Si entran en conflicto (ej: "para llegar a más usuarios tendría que放弃 aprender X"), el primario gana.

#### 1.2. Visión del producto

> "Un gimnasio pequeño (10-200 miembros) debe poder descargar GymFlow Lite, instalarlo en su propio server (o pedir que se lo instalen), operarlo offline-first sin depender de internet, y mantener el control total de su data. Si GymFlow Lite les sirve, pueden donar. Si necesitan ayuda, pueden pagar un servicio. Pero **nunca** deberían estar atados a un proveedor que les cobre recurrente sin alternativa."

#### 1.3. Lo que GymFlow Lite NO es

- **NO es un SaaS** que el maintainer opera. Ver [ADR-007](007-modelo-self-hosted.md).
- **NO es open-core** con features premium pagas en código cerrado. Todo es AGPL v3.
- **NO es un proyecto comercial** con SLAs, soporte 24/7, ni on-call rotation.
- **NO es "WordPress para gimnasios"** (esa frase ha sido usada por la competencia; GymFlow Lite tiene su propia personalidad).

### 2. Licencia y propiedad intelectual

**Decisión**: [GNU Affero General Public License v3.0 (AGPL v3)](https://www.gnu.org/licenses/agpl-3.0.html).

**Justificación completa**: ver [ADR-006](006-licencia-agpl-v3.md).

**Implicancias prácticas**:

- El archivo `LICENSE` en la raíz del repositorio contiene el texto completo de la licencia (ya creado en esta sesión).
- Todo código贡献 acepta la AGPL v3 al贡献.
- Los nombres "GymFlow" y "GymFlow Lite" **no** están cubiertos por la AGPL (la licencia protege código, no marcas). Los forks deben renombrarse.
- Los headers de copyright en archivos fuente deben seguir la convención: `Copyright (C) 2026 GymFlow Lite Contributors`.

### 3. Distribución

**Decisión**: **Self-hosted puro**, distribución por GitHub.

**Justificación completa**: ver [ADR-007](007-modelo-self-hosted.md).

**Implicancias prácticas**:

- **Repositorio oficial**: GitHub (este repo).
- **Método de instalación recomendado**: `git clone` + Docker Compose (`docker compose up -d`).
- **Installer one-liner** (roadmap): `curl install.gymflow.app | bash` para简化.
- **Binarios precompilados** (roadmap opcional): para quienes no quieren Git.
- **Demo online público** (roadmap opcional): una instancia corriendo 24/7 para que la gente pruebe antes de instalar.

### 4. Monetización (oportunista, no agresiva)

El modelo de monetización es **oportunista**: hay canales abiertos para que el dinero llegue, pero **nunca** se presiona al usuario a pagar. El software es gratis. Si el usuario quiere支持, lo hace voluntariamente.

#### 4.1. Canales de monetización

| Canal | Tipo | Cuándo se activa |
|---|---|---|
| **GitHub Sponsors** | Donaciones recurrentes o únicas | Disponible desde el día 1 |
| **OpenCollective** | Donaciones transparentes (con rendición de cuentas pública) | Disponible desde el día 1 |
| **Servicios profesionales** (instalación, migración, custom, soporte) | Pago por hora o paquete | Cuando alguien los pide |
| **Soporte oficial** | Suscripción mensual opcional | Cuando alguien lo pide |
| **Training** | Pago por sesión | Cuando alguien lo pide |
| **Patrocinios corporativos** | Acuerdo personalizado | Si una empresa quiere patrocinar el desarrollo |

#### 4.2. Precios sugeridos (orientativos, ajustables)

Los precios son **sugerencias**, no tarifas. El maintainer puede ajustarlos caso por caso.

**Servicios profesionales**:

| Servicio | Precio sugerido | Tiempo estimado |
|---|---|---|
| Instalación inicial (1 instancia) | USD 200-500 | 2-4 horas |
| Migración desde otro sistema (ej. Excel, otro software) | USD 300-800 | 4-8 horas |
| Custom feature (específica del cliente) | USD 80-150/hora | Variable |
| Setup de producción (HTTPS, backups, monitoring) | USD 400-1000 | 4-8 horas |

**Suscripción de soporte**:

| Plan | Precio mensual | Incluye |
|---|---|---|
| Community | USD 0 | GitHub Issues, Discussions, Discord público |
| Standard | USD 50-100/mes | Respuesta a issues en 48h, video calls de 1h/mes |
| Premium | USD 200-500/mes | Respuesta en 24h, video calls de 4h/mes, hotfixes prioritarios |

**Donaciones (sugerencias, no mínimos)**:

- USD 5/mes: "café para el maintainer"
- USD 20/mes: "sponsor del proyecto"
- USD 100+/mes: "sponsor principal" (mención en el README)

**Nota importante**: estos precios están **calibrados para Latam / mercados con menor poder adquisitivo** (el maintainer es de Argentina). Para clientes de USA/Europa se puede cobrar más.

#### 4.3. Qué NO se hace

- **NO** se hacen descuentos agresivos ni marketing de venta.
- **NO** se hacen contratos de exclusividad ni de soporte con SLAs legales.
- **NO** se promete disponibilidad 24/7 ni tiempos de respuesta garantizados (excepto en el plan Premium, y aún así son "esfuerzo razonable", no SLA).
- **NO** se ofrece consultoría gratuita extensiva. El tiempo del maintainer es finito.

### 5. Gobernanza

#### 5.1. Roles

| Rol | Quién | Responsabilidad |
|---|---|---|
| **Maintainer** (1 persona) | @gymflow-tech-lead (inicial) | Decisiones finales, releases, comunicación oficial |
| **Maintainer co-piloto** (cuando exista) | Por definir | Backup del maintainer, releases conjuntos |
| **Contributor** (cualquiera) | Quien贡献 código, docs, issues, feedback | Sigue el flujo de贡献 |
| **Sponsor** (financiero) | Quien dona o paga servicios | Sin responsabilidad técnica |

#### 5.2. Cómo贡献

El proyecto acepta贡献 de:
- **Código** (PRs al repositorio).
- **Documentación** (PRs a `docs/`).
- **Issues** (bug reports, feature requests).
- **Discusión** (GitHub Discussions, Discord cuando exista).
- **Traducciones** (cuando i18n exista).
- **Plugins de terceros** (publicados en sus propios repos).

#### 5.3. Flujo de contribución

```
1. Issue o Discussion primero
   (para cambios grandes; PRs chiquitos pueden ir directo)
        ↓
2. Fork + branch + cambios
        ↓
3. PR con descripción clara
        ↓
4. Review por el maintainer
        ↓
5. CI pasa (lint + build + tests)
        ↓
6. Merge
```

**Política de revisión**:
- PRs chiquitos (< 50 líneas, sin cambios arquitectónicos): revisión rápida, merge en 1-3 días.
- PRs medianos: 3-7 días.
- PRs grandes / breaking changes: requieren una Discussion previa Y un ADR si es arquitectónico.

#### 5.4. Política de releases

- **Versionado semántico estricto** (MAJOR.MINOR.PATCH).
- **MAJOR** (breaking change): requiere RFC previa y aviso de 2 versiones minor de deprecated.
- **MINOR** (feature nueva): cada 1-3 meses, según churn.
- **PATCH** (bug fix): cuando se acumula suficiente.
- **CHANGELOG.md** mantenido al día.
- **GitHub Releases** con notas estructuradas.

#### 5.5. Code of Conduct (resumen)

- Respeto: cero tolerancia a acoso, discriminación, o trolling.
- Constructividad: feedback es sobre el código, no sobre la persona.
- Paciencia: el maintainer es 1 persona, no espere respuesta inmediata.
- Honestidad: si贡献 algo, declárelo (no plagio, no código generado por IA sin revisión).

(Una versión expandida del CoC vivirá en `CODE_OF_CONDUCT.md` cuando se escriba.)

### 6. Roadmap estratégico

El roadmap está dividido en **3 horizontes temporales** con foco diferente:

#### 6.1. Horizonte 1 — Hacer el proyecto "usable por otros" (próximos 3-6 meses)

**Objetivo**: que un tercero no técnico (o un dev junior) pueda instalar GymFlow Lite y tener su gimnasio funcionando.

| Initiative | Prioridad | Estado |
|---|---|---|
| Sistema de plugins opt-in (HU-015) | 🔴 Alta | Pendiente |
| CLI de GymFlow: install, upgrade, backup, doctor (HU-016) | 🔴 Alta | Pendiente |
| Schema versioning + migraciones aditivas (HU-017) | 🔴 Alta | Pendiente |
| Installer one-liner (Docker Compose pulido) | 🔴 Alta | Pendiente |
| CHANGELOG.md con v1.0.0 | 🟡 Media | Pendiente |
| Demo online público | 🟡 Media | Pendiente |
| Documentación de instalación (paso a paso) | 🟡 Media | Pendiente |
| Documentación de upgrade (versión a versión) | 🟡 Media | Pendiente |
| GitHub Sponsors + OpenCollective activados | 🟢 Baja | Pendiente |

**Criterio de éxito del Horizonte 1**: una persona no técnica puede seguir un tutorial de 30 minutos y tener GymFlow corriendo.

#### 6.2. Horizonte 2 — Hacer el proyecto "viable comercialmente" (6-12 meses)

**Objetivo**: que el maintainer pueda recibir donaciones y cobrar por servicios sin fricción.

| Initiative | Prioridad |
|---|---|
| Landing page del proyecto (qué es, cómo se instala) | 🟡 Media |
| Página de servicios profesionales con precios | 🟡 Media |
| Discord público (comunidad) | 🟡 Media |
| Help center / manual de usuario | 🟡 Media |
| Programa de early adopters (10 gimnasios gratis con feedback a cambio) | 🟡 Media |
| Tests E2E con Playwright | 🟡 Media |
| Observabilidad básica (logs estructurados) | 🟡 Media |
| Demo online con datos sembrados | 🟡 Media |
| i18n: inglés (además de español) | 🟢 Baja |

**Criterio de éxito del Horizonte 2**: el maintainer ha recibido al menos 3 donaciones genuinas y al menos 1 solicitud de servicio profesional.

#### 6.3. Horizonte 3 — Hacer el proyecto "escalable y rico" (12+ meses)

**Objetivo**: que el proyecto tenga una comunidad autosustentable.

| Initiative | Prioridad |
|---|---|
| Marketplace de plugins de terceros | 🟢 Baja |
| Programa de partners / consultores certificados | 🟢 Baja |
| API pública + webhooks | 🟢 Baja |
| Analytics de producto (PostHog self-hosted) | 🟢 Baja |
| Pasarela de pagos embebida (cobros a socios del gimnasio) | 🟢 Baja |
| A/B testing framework | 🟢 Baja |
| (Opcional) SaaS complementario del maintainer con nombre distinto | 🟢 Exploración |

**Criterio de éxito del Horizonte 3**: hay al menos 1 contribuidor externo activo por trimestre Y al menos 1 plugin de terceros publicado.

### 7. Métricas de éxito (cómo medimos si el proyecto "está vivo")

| Métrica | Meta Horizonte 1 | Meta Horizonte 2 | Meta Horizonte 3 |
|---|---|---|---|
| GitHub stars | 10+ | 50+ | 200+ |
| Forks | 5+ | 20+ | 50+ |
| Instalaciones reportadas (issues, Discord, surveys) | 5+ | 20+ | 100+ |
| Contribuidores únicos (code + docs) | 3+ | 10+ | 25+ |
| Releases publicados | 3+ | 8+ | 20+ |
| Donaciones recibidas | 0-1 | 5+ | 30+ |
| Solicitudes de servicios profesionales | 0 | 1+ | 10+ |
| Issues abiertos responded en <7 días | 50%+ | 80%+ | 95%+ |

**Nota**: las métricas de dinero (donaciones, servicios) son **oportunistas**. Si no llegan, no significa que el proyecto falló. La métrica más importante es: **¿el proyecto sigue siendo divertido de mantener + útil para alguien?**

### 8. Riesgos existenciales y plan de salida

| Riesgo | Probabilidad | Impacto | Mitigación / Plan de salida |
|---|---|---|---|
| **Burnout del maintainer** | Alta (proyectos OSS tienen 80% de burnout en 2 años) | El proyecto queda en mantenimiento o abandonado | (1) Mantener scope chiquito. (2) No comprometerse a SLAs. (3) Designar co-maintainer cuando sea posible. (4) Si el maintainer se va, dejar claro el estado en el README y archivar el repo (no borrar). |
| **El proyecto no genera tracción** | Media | Nadie lo usa, donations = 0 | Aceptar que el objetivo primario es aprender. El proyecto sigue siendo válido como "laboratorio de aprendizaje". No es un fracaso. |
| **Un competidor hace fork y crece más** | Media | El proyecto original queda en segundo plano | (1) AGPL v3 protege contra fork closed-source. (2) Si el fork es open también,恭喜 (más visibilidad para todos). (3) Si el fork es mejor,贡献 allí. |
| **Cambio en el entorno técnico** (.NET 8 EOL, etc.) | Alta (en 3-5 años) | Migración costosa | Mantener el stack actualizado. Si la migración es inviable, evaluar reescritura o deprecation. |
| **Conflicto legal con un contribuidor** | Baja | El proyecto queda atascado | (1) Documentar que贡献 = aceptar AGPL v3. (2) Si hay conflicto, mediación antes de litigio. (3) Si no se resuelve, revocar acceso al contribuidor. |
| **El maintainer consigue trabajo comercial con esto** | Baja | El proyecto se vuelve "trabajo" | (a) Si el trabajo comercial es coherente con AGPL v3 + open-source, perfecto. (b) Si no, separar proyectos. |

### 9. Plan de salida (si el maintainer se va)

Si el maintainer decide dejar el proyecto (por burnout, cambio de interés, lo que sea):

1. **Aviso público** con al menos 30 días de anticipación (README + GitHub Discussions + Discord si existe).
2. **Búsqueda de sucesor**: postear en GitHub Discussions y redes buscando co-maintainer o nuevo owner.
3. **Si hay sucesor**: transferencia de ownership en GitHub.
4. **Si NO hay sucesor**: 
   - Marcar el repo como `archived` (no se borra).
   - Actualizar el README indicando el estado.
   - El código sigue siendo AGPL v3 — cualquiera puede forkear.
5. **No borrar NADA**: la historia del proyecto (commits, issues, docs) tiene valor por sí misma.

---

## ⚖️ Alternativas Consideradas

### Alternativa A: SaaS Multi-Tenant (modelo Notion / Glofox)

**Pros**: ingreso recurrente predecible, control total, métricas centralizadas.

**Contras**: requiere equipo de DevOps, soporte 24/7, infraestructura masiva, multi-tenancy refactor. Incompatible con "completamente gratis" y con "1 persona a tiempo parcial". **Rechazada** — ver [ADR-007](007-modelo-self-hosted.md) para análisis completo.

### Alternativa B: Open-Core con SaaS Propio (modelo GitLab)

**Pros**: doble producto (CE gratis + EE pago), ingreso más predecible.

**Contras**: requiere empresa formal, doble mantenimiento, fricción con la comunidad. Incompatible con "completamente gratis". **Rechazada** — ver [ADR-007 sección Alternativa 4](007-modelo-self-hosted.md#opción-4-open-core-con-saas-propio-modelo-gitlabcom--gitlab-ce) para análisis.

### Alternativa C: Hobby sin expectativas (sin governance, sin LICENSE, sin roadmap)

**Pros**: cero overhead, máxima flexibilidad.

**Contras**: nadie sabe qué esperar, no hay reglas de贡献, no hay protección legal. **Rechazada** — el objetivo "tener algo profesional que otros puedan usar" requiere al menos gobernanza mínima.

### Alternativa D: Mantener el código privado / propietario

**Pros**: control total, posible venta futura.

**Contras**: incompatible con "100% open-source" y con "completamente gratis". **Rechazada** — la decisión filosófica del maintainer es open-source.

### Alternativa E: Licencia más permisiva (MIT o Apache 2.0) ✅ (considerada pero rechazada)

**Pros**: máxima adopción, sin fricción empresarial.

**Contras**: **no protege contra fork closed-source**. Un competidor podría tomar el código, hacerlo SaaS privado, y贡献 nada. Esto destruye el incentivo del maintainer. **Rechazada** — AGPL v3 es la opción correcta. Ver [ADR-006](006-licencia-agpl-v3.md) para análisis completo.

---

## 📐 Impacto

### Técnico

- **Sin cambios al código actual**. Esta RFC es de gobernanza, no de implementación.
- **Futuros cambios** (HU-015, HU-016, HU-017) deben ser coherentes con esta RFC.
- **El sistema de plugins opt-in (HU-015)** es el primer cambio técnico que la materializa.
- **El schema versioning + migraciones aditivas (HU-017)** es crítico para que el modelo self-hosted funcione a escala.

### Negocio

- **Posicionamiento claro**: "open-source self-hosted para gimnasios pequeños". Esto filtra adoptantes y enfoca el mensaje.
- **Modelo de monetización oportunista**: bajo riesgo, bajo upside. Si funciona,年收入 puede llegar a 1-5k USD/año en donaciones + 5-20k USD/año en servicios. Si no, el proyecto sigue siendo valioso como aprendizaje.
- **Sin métricas de "growth" agresivas**: el objetivo NO es llegar a 1000 clientes, es tener una comunidad pequeña y satisfecha.

### Equipo

- **El equipo es 1 persona** (el maintainer). Esta RFC no asume más personas.
- **Co-maintainer es OPCIONAL** y solo se busca si el proyecto crece orgánicamente.
- **No hay contractors ni empleados** (sería incompatible con "completamente gratis" — alguien tendría que pagarles).
- **Skills necesarias a futuro** (si llega el crecimiento):
  - DevOps / infra (para el demo online público)
  - Frontend (para mejorar la UI)
  - Diseño gráfico (para marketing y docs)
  - Tech writer (para el manual de usuario)

### Comunidad

- **Mensaje claro**: GymFlow Lite es para quien quiera usar self-hosted. No intentamos ser para todos.
- **Precedente establecido**: Odoo, Nextcloud, GitLab CE muestran que este modelo funciona.
- **Expectativas calibradas**: el maintainer es 1 persona, no se promete SLAs.

---

## 🚧 Riesgos y Mitigaciones

| # | Riesgo | Probabilidad | Impacto | Mitigación |
|---|---|---|---|---|
| R1 | El proyecto no atrae contribuidores externos | Media | El maintainer hace todo solo | (a) Hacer onboarding claro (CONTRIBUTING.md). (b) Etiquetar issues `good first issue`. (c) Ser amable en code review. |
| R2 | Las donaciones son insuficientes para cubrir costos | Alta | Maintainer paga de su bolsillo | (a) Mantener costos al mínimo (VPS chico, dominio barato). (b) Aceptar que es un hobby, no un ingreso. (c) Si los costos se vuelven insostenibles, evaluar sponsorships. |
| R3 | Un contribuidor贡献 código que genera problemas legales | Baja | El proyecto se complica | (a) AGPL v3 + nota de "贡献 = aceptar licencia". (b) Si hay duda, pedir revisión legal. (c) Revocar acceso si es necesario. |
| R4 | El proyecto se vuelve "mainstream" y el maintainer no puede manejarlo | Baja-Media | Burnout | (a) Designar co-maintainer temprano. (b) Si es necesario, declarar "modo mantenimiento" (PRs solo bugfixes). |
| R5 | Cambia la visión del maintainer a mitad de camino | Media | Inconsistencias | Aceptar. Si el maintainer quiere pivotar, abre una Discussion y se discute. Las personas que no estén de acuerdo pueden forkear. |
| R6 | Una empresa grande quiere "comprar" el proyecto | Baja | Cambio de licencia, enojo de la comunidad | AGPL v3 + nota en LICENSE: "GymFlow es de los contribuidores, no se vende". La empresa puede赞助, no comprar. |
| R7 | La licencia AGPL v3 genera fricción con contribuidores corporativos | Media | Algunos no贡献 | Documentar claramente la elección en el README. Si alguien no贡献 por la licencia, está en su derecho. |
| R8 | El roadmap no se cumple por falta de tiempo del maintainer | Alta | Descontentamiento de la comunidad | El roadmap es **orientativo**, no promesa. Si se atrasa, se comunica. No se promete lo que no se puede cumplir. |
| R9 | Aparece un competidor con SaaS y marketing agresivo | Media | Pérdida de mercado (el nicho) | Competir en valor, no en marketing. El self-hosted es nuestro diferenciador; usarlo. |
| R10 | El maintainer se enferma o tiene una emergencia personal | Media | El proyecto queda en pausa | (a) Designar co-maintainer o "trusted committer" para emergencias. (b) El proyecto puede sobrevivir 1-3 meses sin actividad sin morir. |

---

## 🔗 Referencias

- [ADR-006: Licencia AGPL v3](006-licencia-agpl-v3.md) — la decisión de licencia
- [ADR-007: Modelo Self-Hosted vs SaaS](007-modelo-self-hosted.md) — la decisión de distribución
- [ADR-001: Technology Stack](001-stack-tecnologico.md) — el stack
- [ADR-002: Authentication Strategy](002-estrategia-autenticacion.md) — la auth
- [ADR-003: Database Migration Strategy](003-estrategia-migraciones.md) — las migraciones
- [ADR-004: Documentation Structure (FlowDocs)](004-estructura-documentacion-flowdocs.md) — la estructura de docs
- [ADR-005: Naming Conventions](005-convenciones-naming.md) — las convenciones
- [RFC-001: Architecture Offline Sync](../RFC_001_Architecture_Offline_Sync.md) — la decisión técnica vigente
- [PRD_GymFlow_Lite.md](../PRD_GymFlow_Lite.md) — el PRD (será actualizado en Fase B)
- [AGENTS.md](../../AGENTS.md) — reglas operacionales (será actualizado en Fase B)
- [Odoo Community](https://www.odoo.com/page/community) — precedente exitoso del modelo
- [Nextcloud Business Model](https://nextcloud.com/businessmodel/) — precedente open-source self-hosted
- [GitLab Open Source](https://about.gitlab.com/handbook/marketing/developer-relations/open-source/) — precedente open-core + SaaS

---

## 📋 Próximos pasos una vez aceptada esta RFC

1. ✅ Crear archivo `LICENSE` (hecho en esta sesión).
2. ✅ Crear ADR-006 (hecho en esta sesión).
3. ✅ Crear ADR-007 (hecho en esta sesión).
4. **Fase B**: actualizar `AGENTS.md`, `PRD`, `User_Stories_GymFlow.md`, crear `CHANGELOG.md`.
5. **Fase C**: crear HU-014 a HU-017 + ADR-008 (sistema de plugins).
6. **Implementar Fase A del roadmap** (HU-015, HU-016, HU-017).
7. **Activar GitHub Sponsors y OpenCollective**.
8. **Publicar el primer release v1.0.0** con CHANGELOG.

---

**Esta RFC es el "contrato social" del proyecto. Si vos estás de acuerdo con estos términos,贡献. Si no, fork con tu propia visión. La AGPL v3 y la apertura del proyecto lo permiten.**
