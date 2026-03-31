# Estado de Implementación — GymFlow Lite

> Última actualización: 2026-03-31

## Leyenda
- ✅ Completo
- ⚠️ Parcial (detalle en notas)
- ❌ Pendiente

## HUs implementadas

| HU  | Nombre                        | Backend | Frontend | Tests | Estado   | Notas |
|-----|-------------------------------|---------|----------|-------|----------|-------|
| 01  | Validación de Acceso Offline  | ✅      | ✅       | ✅    | Completa |       |
| 02  | Registro de Socio con Foto    | ✅      | ✅       | ✅    | Completa |       |
| 03  | Venta de Producto             | ✅      | ⚠️       | ✅    | Parcial  | UI parcialmente cubierta (falta ProductRow, QuantityEditor, historial). |
| 04  | Sincronización Automática     | ✅      | ✅       | ✅    | Completa | Tests completos; fix en MemberUpdate + stock. |
| 05  | Autenticación y Sesión Offline| ✅      | ✅       | ✅    | Completa |       |
| 06  | Auditoría de Check-ins        | ✅      | ✅       | ✅    | Completa |       |
| 07  | Congelamiento de Membresías   | ✅      | ✅       | ✅    | Completa | Tab integrada en MemberDetail. |
| 08  | Cancelación con Acceso        | ✅      | ✅       | ✅    | Completa | Tab integrada en MemberDetail. |
| 09  | Perfil Antropométrico         | ✅      | ✅       | ✅    | Completa |       |
| 10  | Visualización de Progreso     | ✅      | ✅       | ✅    | Completa |       |
| 11  | Asignación de Rutinas         | ✅      | ✅       | ✅    | Completa |       |

## HUs pendientes

| HU  | Nombre                        | Prioridad PRD |
|-----|-------------------------------|---------------|
| 12  | Dashboard de Métricas         | Fase 2        |

## Fixes recientes

- `Sale.cs` — se agregaron `Create(Guid, Guid, DateTime)`, `AddLine()`, `Complete()`, `ToDto()`
- `syncService.ts` — se agregaron `retryFromErrorQueue()` y `discardFromErrorQueue()`
- `gymflow.db.ts` — `SyncEventType` ahora incluye `'MemberUpdate'`
- `SaleRepository.cs` — código unreachable eliminado en `GetByClientGuidAsync`