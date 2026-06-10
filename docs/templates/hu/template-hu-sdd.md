# HU-XXX: [Título descriptivo]

**Status**: 🟡 In Progress
**Owner**: @usuario
**Created**: YYYY-MM-DD
**Priority**: Must | Should | Could
**Estimación**: S | M | L | XL

---

## 🎯 Intent

¿Por qué existe esta HU? ¿Qué problema resuelve? ¿Qué valor aporta al negocio? (2-3 líneas)

---

## 📋 Scope

### In Scope
- Funcionalidad 1
- Funcionalidad 2
- Funcionalidad 3

### Out of Scope
- Lo que NO se hace en esta HU
- (Para evitar scope creep)

---

## 👥 User Story

**Como** [rol de usuario]
**Quiero** [acción/funcionalidad]
**Para** [beneficio/valor de negocio]

---

## ✅ Requirements

### MUST (obligatorios)
- [ ] Requisito 1
- [ ] Requisito 2

### SHOULD
- [ ] Requisito 3

### COULD
- [ ] Requisito 4

---

## 🧪 Criterios de Aceptación (Given/When/Then)

- [ ] **Given** [contexto]
      **When** [acción]
      **Then** [resultado]
- [ ] **Given** [contexto]
      **When** [acción]
      **Then** [resultado]

---

## 🔗 Dependencias

- Depende de: HU-XXX
- Bloquea: HU-XXX

---

## 📦 Affected Areas

- `src/backend/...`
- `src/frontend/...`
- `docs/...`

---

## 🧪 Verification

- [ ] Tests unitarios
- [ ] Tests integración
- [ ] Tests e2e
- [ ] QA manual
- [ ] Code review

---

## 📝 Notas

---

# Ciclo SDD (Spec-Driven Development)

> Esta HU sigue el flujo completo SDD. Cada fase genera un artefacto.

## 1. Proposal (propuesta)

**Ubicación**: `docs/tasks/HU-XXX-HU-YYY/HU-XXX-proposal.md`

**Contenido**:
- ¿Qué queremos resolver?
- ¿Por qué ahora?
- ¿Qué alternativas consideramos?
- ¿Qué NO está en scope?

## 2. Spec (especificación)

**Ubicación**: `docs/tasks/HU-XXX-HU-YYY/HU-XXX-spec.md`

**Contenido**:
- Requisitos funcionales detallados
- Requisitos no funcionales (performance, seguridad, etc.)
- Casos edge
- Restricciones técnicas

## 3. Design (diseño técnico)

**Ubicación**: `docs/tasks/HU-XXX-HU-YYY/HU-XXX-design.md`

**Contenido**:
- Arquitectura propuesta
- Diagramas de secuencia
- Modelo de datos (si cambia)
- APIs nuevas o modificadas
- Trade-offs considerados

## 4. Tasks (tareas de implementación)

**Ubicación**: `docs/tasks/HU-XXX-HU-YYY/HU-XXX-tasks.md`

**Contenido**:
- Lista de tareas ordenadas
- Cada tarea con criterios de done
- Estimación por tarea

## 5. Apply (implementación)

**Ubicación**: Código en `src/`

**Contenido**:
- Implementar siguiendo tasks.md
- Tests por tarea
- Code review

## 6. Verify (verificación)

**Ubicación**: `docs/tasks/HU-XXX-HU-YYY/HU-XXX-verify.md`

**Contenido**:
- Checklist de criterios de aceptación
- Tests pasando
- QA aprobado
- Deploy a staging/prod

## 7. Archive (archivar)

**Ubicación**: Mover HU-XXX a `docs/tasks/archive/`

**Contenido**:
- HU cerrada
- Link al commit/release
