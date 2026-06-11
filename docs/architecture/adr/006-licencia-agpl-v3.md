# ADR-006: Licencia AGPL v3

**Status**: 🟢 Accepted
**Date**: 2026-06-10
**Deciders**: @gymflow-tech-lead

---

## 🎯 Contexto

GymFlow Lite es un producto de gestión para gimnasios pequeños, distribuido como **self-hosted** (cada cliente corre su propia instancia). El proyecto busca:

- Ser **100% open-source y completamente gratis** para el usuario final.
- Permitir que terceros lo usen, lo modifiquen, lo redistribuyan y hasta cobren por servicios alrededor.
- Probar tecnologías con un vehículo real, no como hobby encapsulado.
- Potencialmente recibir donaciones y ofrecer servicios profesionales si surgen.

La decisión de **licencia** es fundacional: define qué puede hacer la comunidad con el código, qué protecciones tiene el proyecto, y cómo se alinea la licencia con el modelo de negocio elegido.

### Fuerzas en juego

| Fuerza | Detalle |
|---|---|
| **Filosofía personal** | El proyecto es para "probar tecnologías" + "tener algo profesional que otros puedan usar" — la licencia debe reflejar apertura, pero también profesionalismo. |
| **Modelo de negocio oportunista** | Donaciones (GitHub Sponsors, OpenCollective) + servicios profesionales + soporte — todo OPCIONAL, no obligatorio. |
| **Riesgo de fork closed-source** | Un competidor podría tomar el código, hostearlo como SaaS y captar usuarios sin贡献. Eso sería destructivo para el proyecto. |
| **Precedente de la industria** | Odoo Community, Nextcloud, GitLab CE, Mattermost, Discourse, Cal.com — todos open-source con licencias copyleft (AGPL o GPL). Funciona. |
| **Tracción inicial** | Un proyecto personal sin comunidad todavía necesita que la licencia sea "atractiva" para futuros contribuidores, sin asustar a usuarios con copyleft fuerte. |
| **Compatibilidad con la pila tecnológica** | Stack .NET 8 + React + PostgreSQL — todas las dependencias son compatibles con AGPL v3. |

### Restricciones

- El proyecto debe ser **legalmente redistribuible** sin ambigüedad.
- La licencia debe **proteger el trabajo** de apropiación indebida (sobre todo en el caso SaaS).
- No debe requerir **legal overhead** desproporcionado para el maintainer (sin "CLA" complejo, sin "DCO" obligatorio al inicio).
- Debe ser **compatible con la mayoría de las dependencias** del stack.

---

## 🤔 Decisión

**Elegimos**: **GNU Affero General Public License v3.0 (AGPL v3)**.

Es la licencia usada por Odoo Community, Nextcloud, GitLab CE, Mattermost, Discourse, Sentry y Cal.com — todos proyectos open-source exitosos que venden servicios alrededor. Combina la apertura de GPL con la cláusula de "network use is distribution" (sección 13), que obliga a quien ofrece el software como servicio a abrir su código derivado.

El archivo `LICENSE` en la raíz del repositorio contiene el texto completo de la licencia más una nota explicativa de por qué se eligió.

---

## ⚖️ Opciones Consideradas

### Opción 1: MIT

**Pros**:
- Máxima libertad: cualquiera puede hacer lo que quiera, incluyendo cerrar el código.
- Sin "letra chica": fácil de entender, sin abogado necesario.
- Atractiva para adopción empresarial (las empresas aman MIT).
- Compatible con TODO.

**Contras**:
- **Cero protección contra fork closed-source**: un competidor puede tomar GymFlow, hacerlo SaaS, y no贡献 nunca.
- Si el proyecto tiene tracción, el "maintainer original" puede ser eclipsado por el fork comercial.
- Incompatible con el espíritu "el código permanece libre para los usuarios de la red" (sección 13 de AGPL).
- El modelo "donaciones + servicios" funciona peor sin comunidad atada por copyleft.

### Opción 2: Apache 2.0

**Pros**:
- Similar a MIT pero con protección explícita de patentes.
- Aceptada en la industria (Kubernetes, TensorFlow, Swift).
- Atractiva para empresas.

**Contras**:
- **Mismas limitaciones que MIT** respecto a fork closed-source.
- Más verbose que MIT (más para leer, lawyer-friendly).
- La protección de patentes es nice-to-have para un proyecto de gimnasio.

### Opción 3: GPL v3

**Pros**:
- Copyleft fuerte: cualquiera que distribuya binarios modificados tiene que abrir el código.
- Familiar para la comunidad open-source.
- Sin la cláusula de "network use" (más simple que AGPL).

**Contras**:
- **El loophole del SaaS**: alguien puede tomar GymFlow, hacer fork interno, hostearlo como servicio, y NO tiene que abrir NADA (porque no distribuye binarios).
- Justamente el escenario que más queremos evitar.
- Misma "virulencia" que AGPL sin la protección específica.

### Opción 4: AGPL v3 ✅ (elegida)

**Pros**:
- Cierra el **loophole del SaaS**: quien ofrece GymFlow (o derivado) como servicio de red tiene que abrir el código a sus usuarios.
- Copyleft fuerte para distribuciones binarias (como GPL).
- Compatibilidad con GPL: el código AGPL puede ser usado en proyectos GPL (pero no al revés).
- Precedente exitoso: Odoo, Nextcloud, GitLab CE, Mattermost, Discourse, Sentry.
- "Network use is distribution" (sección 13) protege a los usuarios finales, que es el ethos del proyecto.

**Contras**:
- "Compleja" de entender para gente no técnica (las secciones 13 y 7 son densas).
- Algunas empresas la ven como "deal-breaker" (típicamente porque quieren hacer SaaS interno sin abrir nada). Para un proyecto de gimnasio esto es IRRELEVANTE.
- En algunos países, los abogados de las empresas piden revisión legal antes de adoptarla (más fricción que MIT).
- La FSF es la autoridad; si cambian de opinión sobre interpretación, el proyecto queda atado a eso.

### Opción 5: BSL / Licencia comercial personalizada

**Pros**:
- Control total sobre qué se puede y qué no.
- Puedes tener un "core" BSL + "community edition" AGPL (open-core).

**Contras**:
- **Complejidad legal enorme** para un proyecto personal.
- No hay precedente claro en el nicho (gimnasios).
- El modelo open-core (que era nuestra Opción B original) requiere una empresa formal y dos productos diferenciados, lo cual es **incompatible con "completamente gratis"**.
- Requiere abogado para redactar la licencia — costo y tiempo no justificados.

### Opción 6: Dual licensing (MIT + Commercial)

**Pros**:
- Lo mejor de ambos mundos: gratis para la mayoría, paga para empresas que quieren evitar AGPL.

**Contras**:
- Requiere **una entidad legal** que sea dueña del copyright (para poder relicenciar).
- Para un proyecto personal sin empresa, es impracticable.
- Es exactamente el modelo que NO queremos ("completamente gratis" excluye esto).

---

## 📐 Consecuencias

### Positivas

- **El proyecto permanece libre en la red**: si alguien hace SaaS con nuestro código, tiene que abrir su código a sus usuarios. Esto protege la inversión del maintainer y la filosofía del proyecto.
- **Precedente de la industria**: Odoo (€200M+/año facturando con servicios alrededor de Odoo Community AGPL) demuestra que el modelo funciona.
- **Compatibilidad con GPL**: si alguien quiere usar GymFlow en un proyecto GPL, puede.
- **Atracción de contribuidores serios**: las personas que valoran la apertura copyleft tienden a贡献 más a proyectos copyleft.
- **Defensa contra apropiación**: si una empresa grande quiere "comprar" el proyecto, no puede — la licencia les obliga a contribuir de vuelta.
- **Mensaje claro**: la licencia ES la posición filosófica del proyecto. Quien la lee entiende qué defendemos.

### Negativas

- **Fricción de adopción empresarial**: algunas empresas (típicamente grandes) tienen políticas internas que prohíben AGPL. Esto **es esperado y aceptable** — no es nuestro target principal (gimnasios pequeños, no enterprises).
- **Complejidad del texto**: las secciones 7 (additional terms) y 13 (network interaction) son densas. Hay que aceptar que la mayoría de la gente NO va a leer la licencia entera.
- **No tenemos CLA / DCO**: en el futuro, si el proyecto crece y queremos relicenciar, necesitaremos que todos los contribuidores firmen un CLA. Por ahora, no hace falta (AGPL es estable).
- **El nombre "GymFlow Lite" NO está protegido por la licencia**: la AGPL protege el CÓDIGO, no la marca. Si alguien hace un fork, tiene que llamarlo distinto (lo aclaramos en el LICENSE).

### Neutras

- **El header de copyright** en cada archivo fuente debe decir algo como `Copyright (C) 2026 GymFlow Lite Contributors`. No es requerido por la AGPL, pero es la práctica recomendada para que el copyright quede claro.
- **El proyecto está atado a la FSF**: si la Free Software Foundation cambia de licencia (poco probable pero posible), el proyecto puede necesitar un ADR para migrar.
- **Las dependencias deben ser compatibles**: las actuales (Microsoft .NET, React, MUI, PostgreSQL, etc.) todas son MIT/Apache, lo cual es compatible. Hay que vigilar al agregar dependencias nuevas.
- **Los contributores aceptan AGPL v3 al贡献**: esto es estándar (contribuir = aceptar la licencia del proyecto). No requiere firma individual mientras no haya CLA.

---

## 🔗 Referencias

- **Texto oficial**: <https://www.gnu.org/licenses/agpl-3.0.html>
- **TL;DR no oficial**: <https://choosealicense.com/licenses/agpl-3.0/>
- **Odoo Community License FAQ**: <https://www.odoo.com/documentation/16.0/legal/licenses/licenses.html>
- **AGPL v3 vs GPL v3 — Diferencias clave**: <https://www.gnu.org/licenses/why-affero-gpl.html>
- **Precedentes del modelo**: Odoo, Nextcloud, GitLab CE, Mattermost, Discourse, Sentry, Cal.com
- **ADR-007**: Modelo Self-Hosted vs SaaS (decisión de distribución que esta licencia protege)
- **RFC-002**: Modelo de Negocio y Gobernanza (cómo encaja AGPL v3 con donaciones + servicios)
- **AGENTS.md sección 6**: Convenciones operacionales (de dónde sale el flujo de contribución)
