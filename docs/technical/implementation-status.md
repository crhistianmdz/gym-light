# Estado de Implementación — GymFlow Lite

> Última actualización: 2026-04-13

## Leyenda
- ✅ Completo
- ⚠️ Parcial (detalle en notas)
- ❌ Pendiente

## HUs implementadas

| HU  | Nombre                        | Backend | Frontend | Tests | Estado   | Notas |
|-----|-------------------------------|---------|----------|-------|----------|-------|
| 01  | Validación de Acceso Offline  | ✅      | ✅       | ✅    | Completa |       |
| 02  | Registro de Socio con Foto    | ✅      | ✅       | ✅    | Completa |       |
| 03  | Venta de Producto             | ✅      | ✅       | ✅    | Completa | UI parcialmente cubierta (falta ProductRow, QuantityEditor, historial). |
| 04  | Sincronización Automática     | ✅      | ✅       | ✅    | Completa | Tests completos; fix en MemberUpdate + stock. |
| 05  | Autenticación y Sesión Offline| ✅      | ✅       | ✅    | Completa |       |
| 06  | Auditoría de Check-ins        | ✅      | ✅       | ✅    | Completa |       |
| 07  | Congelamiento de Membresías   | ✅      | ✅       | ✅    | Completa | Tab integrada en MemberDetail. |
| 08  | Cancelación con Acceso        | ✅      | ✅       | ✅    | Completa | Tab integrada en MemberDetail. |
| 09  | Perfil Antropométrico         | ✅      | ✅       | ✅    | Completa |       |
| 10  | Visualización de Progreso     | ✅      | ✅       | ✅    | Completa |       |
| 11  | Asignación de Rutinas         | ✅      | ✅       | ✅    | Completa |       |
| 12  | Dashboard de Métricas         | ✅      | ✅       | ✅    | Completa |       |

## HUs pendientes

| HU  | Nombre                        | Prioridad PRD |
|-----|-------------------------------|---------------|

## Fixes recientes

- `Sale.cs` — se agregaron `Create(Guid, Guid, DateTime)`, `AddLine()`, `Complete()`, `ToDto()`
- `syncService.ts` — se agregaron `retryFromErrorQueue()` y `discardFromErrorQueue()`
- `gymflow.db.ts` — `SyncEventType` ahora incluye `'MemberUpdate'`
- `SaleRepository.cs` — código unreachable eliminado en `GetByClientGuidAsync`
- `EnsureCreatedAsync()` en lugar de `MigrateAsync()` — no hay migraciones EF Core formales (deuda técnica conocida)
- `LocalPhotoStorageService` creado en `src/backend/Infrastructure/Services/` — implementación dev de `IPhotoStorageService`
- `SaleLine.Subtotal` ignorada con `entity.Ignore()` en DbContext — es computed property sin setter
- `DomainException.cs` creado en `src/backend/Domain/Exceptions/`
- `ClaimsPrincipalExtensions.cs` y `AuthExtensions.cs` creados en `src/backend/WebAPI/Extensions/`
- 120+ errores TypeScript corregidos → 0 errores
- `EntityTable<T,K>` (Dexie 4.x) reemplazado por `Table<T, string>` — Dexie instalada es v3.x
- Tests excluidos del build de producción en `tsconfig.json`
- `vite-env.d.ts` creado
- `productService.ts` — agregado método `getProducts()`
- `SaleResponse` — agregado campo `isOffline?: boolean`
- `cancelService.ts`, `measurementService.ts`, `saleService.ts` — `sync_queue.add()` corregido: payload como `JSON.stringify()` y campo `isOffline: true`
- Stack Docker completamente funcional: 4 servicios healthy (backend :5000, frontend :3000, postgres, redis)