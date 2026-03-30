# Backlog de Historias de Usuario - GymFlow Lite (Versión Final)

## Épica 1: Operación Base y Disponibilidad (Fase 1)

### HU-06: Auditoría de Check-ins
**Como** Administrador, **quiero** auditar los logs de acceso, **para** detectar anomalías y garantizar la trazabilidad total de check-ins.

#### Criterios de Aceptación
1. **Filtros:** Admin/Owner puede filtrar registros por rango de fechas, recepcionista (performedByUserId), socio (memberId) y resultado (Allowed/Denied).
2. **Exportación:** Permite exportar los datos filtrados en formato CSV correctamente.
3. **Validación:** Check-in que reciba un `performedByUserId` vacío devuelve un error `400 Bad Request` y no persiste el registro.
4. **Idempotencia:** Logs duplicados basados en `ClientGuid` son ignorados gracias al índice único.
5. **Restricciones:** Solo Admin y Owner pueden acceder al panel, mientras que Receptionist recibe un 403 Forbidden.
6. **Retención de datos:** Solo aparecen registros con menos de 3 años (archivados automáticamente al superar este límite).