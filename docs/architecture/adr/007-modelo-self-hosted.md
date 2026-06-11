# ADR-007: Modelo Self-Hosted vs SaaS

**Status**: 🟢 Accepted
**Date**: 2026-06-10
**Deciders**: @gymflow-tech-lead

---

## 🎯 Contexto

GymFlow Lite es un producto de gestión para gimnasios pequeños. La decisión de **cómo se distribuye y opera** define la arquitectura, el modelo de negocio, el esfuerzo de mantenimiento y la relación con los usuarios.

Hay dos modelos canónicos en la industria del software de gestión:

1. **SaaS (Software as a Service)**: el proveedor corre UNA instancia de la app que sirve a múltiples clientes. Los clientes pagan suscripción y el proveedor maneja la infraestructura.
2. **Self-hosted**: el cliente descarga e instala la app en su propia infraestructura (o la de un tercero contratado). El cliente es dueño de su instancia y su data.

Un tercer modelo híbrido (open-core con SaaS propio) lo trataremos en las "Opciones Consideradas".

### Fuerzas en juego

| Fuerza | Detalle |
|---|---|
| **Naturaleza del proyecto** | Proyecto personal, sin empresa formal, sin empleados, sin infraestructura de producción propia (todavía). El maintainer es un dev senior. |
| **Objetivo dual** | Probar tecnologías + tener un producto profesional que otros puedan usar. El "uso por otros" no implica necesariamente que el maintainer los atienda. |
| **Filosofía de código abierto** | El proyecto es "100% open-source y completamente gratis" — no hay����������������. El usuario debe poder tener SU instancia sin depender del maintainer. |
| **Recursos disponibles** | 1 persona, tiempo parcial, sin datacenter, sin DevOps dedicado, sin caja para pagar servers en producción masiva. |
| **Target de cliente** | Gimnasios de 10-200 miembros, con conectividad inestable, a menudo UN solo local físico. Propietarios que valoran control y no quieren atarse a un proveedor. |
| **Riesgo de apropiación** | Si el proyecto es SaaS y crece, un competidor con infraestructura puede hacer fork + cerrar el código. Si es self-hosted, esto es menos probable (la barrera de entrada es alta). |
| **Complejidad de operar SaaS** | Un SaaS real requiere: multi-tenancy, observabilidad, on-call, status page, billing, soporte 24/7, legal, seguridad, compliance. **No es proporcional** al objetivo del proyecto. |
| **Complejidad de operar self-hosted** | Un self-hosted requiere: excelente documentación de instalación, schema versioning robusto, migraciones aditivas, CLI. **Es proporcional** y ayuda al objetivo de "probar tecnologías". |
| **Precedente de la industria** | Odoo Community (self-hosted + SaaS opcional), Nextcloud (self-hosted), GitLab CE (self-hosted + SaaS opcional), WordPress (self-hosted), Glofox (SaaS puro), Mindbody (SaaS puro). Ambos modelos funcionan. |

### Restricciones

- El proyecto debe poder **mantenerse con esfuerzo de 1 persona** a tiempo parcial.
- El código debe ser **legalmente accesible** sin ambigüedad.
- La distribución debe **escalar orgánicamente** (sin que el maintainer sea el cuello de botella de cada instalación).
- El modelo debe **no requerir un equipo legal/contable** para operar.
- Debe ser **fiel a la visión** del maintainer: "si alguien lo quiere usar, que lo use; si lo quiere instalar, se lo instalo; si quiere donar, que done".

---

## 🤔 Decisión

**Elegimos**: **Self-Hosted (cada cliente corre su propia instancia)** con la posibilidad de **servicios profesionales opcionales** alrededor (instalación, migración, custom, soporte, training). NO vamos a ofrecer un SaaS oficial del proyecto.

En términos concretos:

- **Distribución**: el código está en GitHub, cualquiera puede clonarlo o forkearlo.
- **Instalación**: el cliente (o un tercero) corre la app en un server propio. Soportamos Docker Compose como método recomendado.
- **Actualizaciones**: el cliente decide cuándo upgrade. La herramienta `gymflow upgrade` aplica migraciones automáticamente, con backup pre-upgrade.
- **Soporte**: el maintainer ofrece servicios profesionales PAGOS (instalación, custom, soporte) si alguien los pide. NO hay un SLA implícito.
- **Donaciones**: GitHub Sponsors + OpenCollective disponibles, sin presión.
- **SaaS complementario (opcional, futuro)**: si en el futuro queremos ofrecer un SaaS oficial (estilo GitLab.com), sería un **proyecto separado** ("GymFlow Cloud") con un nombre distinto, no una re-venta de GymFlow Lite.

---

## ⚖️ Opciones Consideradas

### Opción 1: SaaS Multi-Tenant (modelo Notion / Slack / Glofox)

**Descripción**: Mantainer corre UNA instancia del backend + UNA DB PostgreSQL + UNA app frontend. Los clientes son "tenants" (gimnasios) que comparten infraestructura. Aislamiento lógico por `gym_id` o similar.

**Pros**:
- **Ingreso recurrente predecible** (suscripciones mensuales/anuales).
- Onboarding y upgrades instantáneos para el cliente.
- Mantainer tiene control total del entorno de producción.
- Métricas de uso centralizadas (qué funciona, qué no).
- Es el modelo "estándar" de SaaS moderno.

**Contras**:
- **Requiere TODO**: multi-tenancy, billing (Stripe), observabilidad (Serilog + Sentry), status page, soporte 24/7, legal (Términos, Privacy, GDPR), backups automatizados, on-call rotation, security audit, penetration testing, **un equipo de DevOps/SRE**.
- **Riesgo de data leak** entre clientes si una query olvida el filtro de `gym_id`.
- **Costo de infraestructura** recae en el maintainer (a 50+ clientes pequeños, los números no cierran sin escala seria).
- **Complejidad arquitectónica enorme**: el código actual habría que reescribirlo entero (multi-tenancy NO es un wrapper, es un cambio en cada entidad, cada repo, cada controller).
- **Incompatible con el objetivo "probar tecnologías"**: SaaS es 90% operación, 10% innovación.
- **Incompatible con "completamente gratis"**: el SaaS tiene costo operativo real (servers, DB, backups) que alguien tiene que pagar.
- **Incompatible con AGPL v3 para el SaaS**: si el maintainer ofrece SaaS con código AGPL, técnicamente tiene que publicar SU código del SaaS también (sección 13). No es bloqueante, pero es raro.

### Opción 2: SaaS Multi-Database (modelo Odoo.com / Salesforce)

**Descripción**: Mantainer corre UN server de aplicaciones + UN server PostgreSQL, pero cada cliente tiene SU database. Aislamiento físico. Mismo código de aplicación, distintos configs.

**Pros**:
- Mejor aislamiento que multi-tenant (cada cliente SU DB).
- Backups por cliente más fáciles.
- Compliance enterprise más fácil de demostrar ("SU DB está aislada").
- Precedente exitoso (Odoo SA factura €200M+/año con este modelo).

**Contras**:
- **Sigue siendo SaaS**, con todos los costos operativos de Opción 1.
- **Más complejo operacionalmente**: N DBs que mantener, N migraciones que coordinar, N backups que verificar.
- **Riesgo de "no escala"** con DBs pequeñas: a 100 clientes con DBs de 50MB cada una, estás pagando 100x el overhead.
- **No resuelve el problema de recursos**: necesita SRE, DevOps, soporte.
- El maintainer no quiere operar un SaaS (lo dice explícitamente: "si alguien lo quiere instalar, se lo instalo").

### Opción 3: Self-Hosted Puro (modelo WordPress / Nextcloud / Odoo Community) ✅ (elegida)

**Descripción**: NO hay servidor del maintainer. El código se distribuye como un paquete (Docker Compose, binarios, código fuente). Cada cliente (o un tercero contratado) corre su propia instancia. El maintainer ofrece servicios profesionales OPCIONALES.

**Pros**:
- **Costo operativo CERO para el maintainer** (no hay servers que mantener).
- **Escala orgánicamente**: el éxito depende de la calidad del producto, no del tamaño del equipo de ops.
- **Compatible con "100% open-source, completamente gratis"**: no hay�� explícita.
- **Compatible con "probar tecnologías"**: el maintainer controla su tiempo, decide cuándo y qué feature construir.
- **Compatible con AGPL v3**: cada cliente tiene SU instancia, no hay sección 13 que aplique.
- **Compatible con "donaciones + servicios oportunistas"**: las donaciones se justifican si el producto es útil, los servicios los paga quien los pide.
- **Precedente exitoso masivo**: Odoo Community, Nextcloud, GitLab CE, WordPress, Ghost, Discourse, Cal.com.
- **El cliente tiene control total de su data**: crítico para un producto de gestión que maneja datos de socios.
- **El maintainer mantiene su tiempo libre**: no hay 3am pages, no hay status pages que mantener.

**Contras**:
- **El cliente debe tener capacidad técnica** (o pagar a alguien que la tenga) para instalar y mantener.
- **Onboarding más lento**: instalar es un proceso, no un click.
- **Updates son responsabilidad del cliente**: si no actualiza, se queda con bugs/features viejos.
- **El maintainer tiene menos control** sobre el entorno de producción.
- **El ingreso es variable y oportunista**: las donaciones son pocas, los servicios solo si alguien los pide.
- **El "éxito" se mide en stars, downloads, no en MRR**: métrica menos tangible.
- **Puede ser percibido como "menos profesional"** por el mercado enterprise (que espera SaaS).

### Opción 4: Open-Core con SaaS Propio (modelo GitLab.com + GitLab CE)

**Descripción**: Hay un "core" open-source (CE) y una versión "enterprise" con features premium (EE) que el maintainer vende como SaaS (GitLab.com) y como self-hosted (GitLab EE).

**Pros**:
- Ingreso más predecible (venden EE + hosting).
- El core open-source sigue siendo útil.
- Precedente exitoso (GitLab facturó >$500M con este modelo).

**Contras**:
- **Requiere DOBLE producto**: mantener CE y EE en paralelo.
- **Requiere una empresa formal** con estructura legal, contable, de ventas.
- **Incompatible con "completamente gratis"**: el tier EE es pago.
- **Complejidad enorme** para un proyecto personal.
- **Riesgo de "ce/fork"**: la comunidad open-source tiende a desconfiar del open-core (les preocupa que el maintainer "mueva" features del CE al EE).

### Opción 5: Híbrido (Self-Hosted + SaaS Complementario del Maintainer)

**Descripción**: El proyecto principal es self-hosted (GRATIS). Eventualmente, el maintainer podría ofrecer un SaaS oficial (PAGO) como un producto SEPARADO con nombre distinto, sin tocar el código self-hosted.

**Pros**:
- Lo mejor de ambos mundos: el self-hosted sigue siendo gratis y la base de la comunidad.
- El SaaS es opt-in para quien quiera pagar por la conveniencia.
- El maintainer puede experimentar con SaaS sin poner en riesgo el proyecto open-source.
- Compatible con AGPL v3 (el SaaS sería un proyecto separado, no una "re-venta" del open-source).

**Contras**:
- **Doble trabajo** si se hace: mantener self-hosted Y SaaS.
- **Riesgo de confusión de marca**: el cliente no sabe si "GymFlow" es el open-source o el SaaS.
- **El SaaS propio es un proyecto ENORME** por sí solo (multi-tenancy, billing, soporte, etc.).
- **No es necesario AHORA**: el modelo self-hosted es suficiente para validar el producto.

**Nota**: dejamos la puerta abierta para explorar este modelo en el futuro (ver RFC-002 sección "Roadmap"), pero NO es la decisión de hoy.

---

## 📐 Consecuencias

### Positivas

- **El maintainer mantiene su tiempo libre**: no hay 3am pages, no hay servers caídos, no hay clientes enojados exigiendo reembolsos. El proyecto se mantiene por interés + valor a la comunidad.
- **Costo operativo CERO**: ningún server, ninguna DB, ningún servicio de pago recurrente. El único costo "indirecto" es el tiempo del maintainer.
- **Escala sin techo de costos**: si 10, 100 o 1000 personas lo instalan, el maintainer no paga más.
- **El cliente tiene soberanía total de su data**: crítico para datos de socios (PII).
- **Compatible con la filosofía**: open-source, gratis, "si alguien lo quiere instalar se lo instalo".
- **El sistema de plugins que viene (HU-015) tiene más sentido** en self-hosted: cada cliente puede activar/desactivar módulos sin que el maintainer tenga que mantener N variantes.
- **El schema versioning + migraciones aditivas (HU-017) son críticos** para self-hosted: sin esto, cada cliente queda atascado en una versión vieja.
- **Aprendizaje técnico real**: self-hosted bien hecho enseña Docker, packaging, distribución, observabilidad desde el lado del cliente (no del operador del SaaS).

### Negativas

- **El "éxito" se mide de forma menos tangible**: stars, downloads, forks, contributors, NO MRR ni users activos. Es más difícil saber si el proyecto "está funcionando".
- **El ingreso es oportunista**: donaciones + servicios solo si llegan. No hay "sueldo" garantizado.
- **El cliente debe tener capacidad técnica** o pagar a alguien que la tenga. Esto filtra a cierto tipo de adoptantes.
- **Riesgo de "miles de versiones en producción"**: cada cliente corre SU versión. Si hay 100 clientes, puede haber 30 versiones distintas en uso, cada una con su set de bugs/features.
- **El maintainer tiene menos control** sobre el entorno del cliente. Si un cliente rompe su instalación, la culpa puede caer en el maintainer aunque no sea así.
- **El feedback loop es más lento**: el cliente no te dice "el botón no funciona", te dice "el gimnasio se quejó" 3 semanas después.

### Neutras

- **La distribución es Docker Compose + binarios**. La instalación requiere capacidad técnica básica (o un servicio profesional).
- **El cliente es responsable de sus backups** (con herramientas provistas por el proyecto, ej. `gymflow backup`).
- **El upgrade es responsabilidad del cliente** (con herramienta `gymflow upgrade` provista por el proyecto).
- **El modelo de plugins opt-in (HU-015) y el schema versioning (HU-017) son CRÍTICOS** para que el self-hosted funcione bien — son pre-requisitos arquitectónicos.
- **El "soporte" es bifurcación**: hay canales públicos (GitHub Discussions, Discord) y canales pagos (email, consultoría). El maintainer decide caso por caso.
- **El proyecto puede pasar a SaaS complementario en el futuro** sin contradecir esta decisión (sería un proyecto aparte, no una re-venta).

---

## 🔗 Referencias

- **Odoo Community + Odoo.sh**: <https://www.odoo.com/page/community> — modelo self-hosted puro, factura €200M+/año
- **Nextcloud**: <https://nextcloud.com/> — self-hosted puro, modelo de negocio basado en soporte
- **GitLab CE + GitLab.com**: <https://about.gitlab.com/pricing/> — modelo open-core con SaaS opcional
- **Ghost (Pro + self-hosted)**: <https://ghost.org/pricing/> — modelo similar
- **Discourse self-hosted + hosting oficial**: <https://www.discourse.org/pricing/>
- **Cal.com (open-source + Cal.com Cloud)**: <https://cal.com/pricing>
- **AGPL v3 sección 13** (Network Interaction) — por qué esta decisión es compatible con AGPL v3
- **ADR-006**: Licencia AGPL v3 — la licencia que protege este modelo
- **RFC-002**: Modelo de Negocio y Gobernanza — cómo encaja self-hosted con donaciones + servicios
- **HU-015**: Sistema de módulos/plugins opt-in — pre-requisito técnico
- **HU-017**: Schema versioning + migraciones aditivas — pre-requisito técnico
- **PRD_GymFlow_Lite.md sección 1** — visión del producto (actualizada en Fase B)
