# HU-06 — Auditoría / Trazabilidad de Check-ins

## Overview
La historia de usuario HU-06 incorpora la funcionalidad para auditar los check-ins realizados por los socios. Permite filtrar registros por varios criterios, exportarlos en formato CSV y garantiza idempotencia mediante restricciones únicas de base de datos.

## Architecture

### Backend
- **Capas involucradas:**
  - `Domain`: Modelo `AccessLogFilter` utilizado como criterio de búsqueda.
  - `Infrastructure`: Repositorio `IAccessLogRepository` con soporte para paginación y retención.
  - `Application`: Casos de uso `GetAccessLogsUseCase` y `ExportAccessLogsUseCase`.
  - `WebAPI`: Controlador `AccessLogsController` provee los endpoints correspondientes.
- **Entidades clave:** `AccessLog`, `AccessLogDto`, `PagedResultDto`.
- **Endpoints:**
  - `GET /api/admin/access-logs`: Consulta paginada con soporte para filtros.
  - `GET /api/admin/access-logs/export`: Exportación en CSV (PDF futuro).

### Frontend
- **Componentes:**
  - `AccessLogsPanel`: Tabla con filtros, paginación y exportación configurada.
- **Servicios:**
  - `adminService.getAccessLogs`: Realiza la consulta paginada.
  - `adminService.exportAccessLogs`: Descarga la exportación en formato solicitado.
- **Tipos:**
  - `AccessLogDto`, `AccessLogFilter`, `PagedResult.`

## API Reference

### GET /api/admin/access-logs
- **Query Params:**
  - `fromDate`, `toDate` (rango de fechas).
  - `performedByUserId` (UUID).
  - `memberId` (UUID).
  - `result` (Allowed, Denied).
- **Respuesta:**
  - `PagedResultDto<AccessLogDto>`
- **Autenticación:** Roles `Admin, Owner` obligatorios.

### GET /api/admin/access-logs/export
- **Query Params:**
  - Igual a los de `GET /access-logs`.
  - `format`: `csv` | `pdf`.
- **Respuesta:**
  - Descargar archivo binario con tipo MIME dependiendo del formato elegido.
- **Autenticación:** Roles `Admin, Owner` obligatorios.

## Data Retention
Los registros son almacenados por un periodo de 3 años. Cada consulta utiliza el campo `CreatedAt` como criterio: `CreatedAt >= UtcNow.AddYears(-3)`.

## Idempotency
Se eliminan entradas duplicadas mediante un índice único en la columna `ClientGuid` dentro de `AccessLogs`. Requests con duplicados existentes retornan un código `200 OK` sin reprocesar el registro.

## Security
- Solo roles `Admin` y `Owner` tienen acceso a los endpoints.
- Se bloquean operaciones en check-ins con `performedByUserId` vacío devolviendo `400 Bad Request`.

## Files Changed
### Backend:
- `Domain`: AccessLogFilter.cs
- `Infrastructure`: SqlAccessLogRepository.cs
- `Application`: GetAccessLogsUseCase.cs, ExportAccessLogsUseCase.cs
- `WebAPI`: AccessLogsController.cs
- Migration: AddUniqueIndexToAccessLogClientGuid

### Frontend:
- `AccessLogsPanel.tsx`
- `adminService.ts`
- `types/accessLog.ts`