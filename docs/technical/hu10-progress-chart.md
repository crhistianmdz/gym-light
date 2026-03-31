# HU-10 — Visualización de Evolución (Gráficas)

## Overview

Permite al Socio (y roles autorizados) visualizar la evolución de sus medidas físicas en una gráfica de líneas interactiva. Es una feature **exclusivamente de frontend** — no introduce cambios en el backend ni en la base de datos.

**Dependencia:** Consume el endpoint `GET /api/members/{memberId}/measurements` implementado en HU-09.

---

## Archivos Creados / Modificados

| Archivo | Tipo | Descripción |
|---------|------|-------------|
| `src/frontend/src/types/progressChart.ts` | Nuevo | Tipos, constantes y utilidades de transformación de datos para la gráfica |
| `src/frontend/src/hooks/useMemberProgress.ts` | Nuevo | Hook que llama `measurementService.getMeasurements()` con fallback offline |
| `src/frontend/src/components/ProgressChart/ProgressChart.tsx` | Nuevo | Componente de gráfica de líneas (recharts + MUI) con selector de variable |
| `src/frontend/src/pages/MemberProgress/MemberProgress.tsx` | Nuevo | Página que monta el chart, aplica RBAC y maneja loading/error |
| `src/frontend/src/pages/MemberDetail/MemberDetail.tsx` | Modificado | Agrega tab "Progreso" y ruta `/progress` |
| `src/frontend/src/__tests__/progressChart.test.ts` | Nuevo | Tests unitarios de utilidades de gráfica |
| `docs/User_Stories_GymFlow.md` | Modificado | HU-10 expandida con 7 CA + decisiones PO |
| `docs/RFC_001_Architecture_Offline_Sync.md` | Modificado | Versión 1.3 + sección 6 Visualización de Datos |
| `docs/technical/api-reference.md` | Modificado | Nota de consumo del endpoint GET /measurements |

---

## Librería de Gráficas

**`recharts`** — seleccionada por decisión del PO (2026-03-31).

| Criterio | recharts | chart.js | victory |
|---------|----------|----------|---------|
| API | JSX declarativa | Imperativa (Canvas) | Declarativa |
| Bundle | ~150kb | ~200kb | ~180kb |
| React-first | ✅ | ❌ (wrapper) | ✅ |
| Adopción | Alta | Muy alta | Media |

---

## Arquitectura del Feature

```
MemberDetail (tab router)
  └── MemberProgress (page)
        ├── useAuth()             → RBAC guard
        ├── useMemberProgress()   → data fetching
        │     └── measurementService.getMeasurements()
        │           ├── Online → GET /api/members/{id}/measurements
        │           └── Offline → IndexedDB (measurements store)
        └── ProgressChart (component)
              ├── MUI Select       → selector de variable
              ├── recharts LineChart → gráfica
              └── buildChartData() → transformación de datos
```

---

## RBAC

| Rol | Puede ver |
|-----|-----------|
| `Member` | Solo sus propias medidas (`userId === memberId`) |
| `Trainer` | Cualquier socio |
| `Admin` | Cualquier socio |
| `Owner` | Cualquier socio |

El guard se aplica en `MemberProgress.tsx` usando `useAuth()`.

---

## Variables Graficables

| Campo | Label | Métrico | Imperial |
|-------|-------|---------|---------|
| `weightKg` | Peso | kg | lbs |
| `bodyFatPct` | % Grasa | % | % |
| `chestCm` | Pecho | cm | in |
| `waistCm` | Cintura | cm | in |
| `hipCm` | Cadera | cm | in |
| `armCm` | Brazo | cm | in |
| `legCm` | Pierna | cm | in |

---

## Estados de la Gráfica

| Condición | Comportamiento |
|-----------|---------------|
| 0 tomas | Mensaje: "Aún no hay medidas registradas." |
| 1 toma | Punto visible (`dot`), sin línea (`strokeWidth: 0`) |
| 2+ tomas | `LineChart` completa con línea + puntos |

---

## Unidades Mixtas

Los valores se muestran **tal como fueron registrados** — sin conversión.
Cada punto del tooltip muestra su propia unidad derivada del `UnitSystem` de esa toma específica.

```typescript
// Ejemplo: toma métrica
{ date: '15/01/2026', value: 75.5, unit: 'kg' }

// Ejemplo: toma imperial
{ date: '10/02/2026', value: 165.0, unit: 'lbs' }
```

---

## Offline-First

El componente usa `useMemberProgress` → `measurementService.getMeasurements()`, que implementa el fallback offline de HU-09:
- Online → `GET /api/members/{id}/measurements` + actualiza cache IndexedDB
- Offline → lee directamente el store `measurements` de Dexie.js, ordenado por `recordedAt` ASC

---

## Ruta de Navegación

```
/members/:id/progress  →  MemberProgress
```

Tab registrado en `MemberDetail.tsx` junto al tab existente "Antropometría".

---

## Decisiones de Diseño

1. **Datos ordenados ASC**: `useMemberProgress` ordena por `recordedAt` ascendente para que la gráfica muestre el tiempo de izquierda a derecha correctamente.
2. **Sin conversión de unidades**: la conversión es responsabilidad del usuario al registrar. El chart refleja fielmente lo registrado.
3. **`strokeWidth: 0` para punto único**: en lugar de ocultar la línea con lógica condicional de recharts, se pasa `strokeWidth={0}` para suprimir la línea pero mantener el punto renderizado.
4. **Tooltip como MUI Card**: consistencia visual con el resto del sistema Material Design.
