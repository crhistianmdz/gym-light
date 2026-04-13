# Migration: AddClientGuidIndexToSales

## Descripción
Agrega un índice en la columna `ClientGuid` de la tabla `Sales` para optimizar las búsquedas de idempotencia.

## Cambios
- Índice en `Sales.ClientGuid` (no unique — puede haber múltiples intentos del mismo ClientGuid antes de que sea procesado, pero el primero exitoso lo marca)
- Nota: La unicidad se garantiza por lógica de negocio en el use case, no a nivel DB constraint.

## Comando
```bash
dotnet ef migrations add AddClientGuidIndexToSales --project src/backend/Infrastructure --startup-project src/backend/WebAPI
dotnet ef database update --project src/backend/Infrastructure --startup-project src/backend/WebAPI
```