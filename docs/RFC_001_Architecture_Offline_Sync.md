# RFC 001: Arquitectura de Sincronización, Idempotencia y Resiliencia

**Estado:** Aprobado para Implementación  
**Versión:** 1.2  
**Tecnologías:** .NET 8, React, Dexie.js (IndexedDB), Service Workers, material design.

## 1. Introducción
Este documento define la estrategia técnica para garantizar que GymFlow Lite opere con un modelo "Offline-First". El sistema prioriza la **Disponibilidad** sobre la **Consistencia Fuerte** inmediata, utilizando sincronización asíncrona e idempotente.

### HU-06: Auditoría de Check-ins
* **UNIQUE Constraint:** La columna `ClientGuid` en `AccessLogs` garantiza idempotencia y bloquea registros duplicados, implementado mediante un índice único en la base de datos.
* **Validación:** Se exige que el campo `performedByUserId` sea no nulo y se valida en el caso de uso correspondiente antes de interactuar con la base de datos.
* **Enfoque de sincronización:** Se extendió el servicio `updateLocalCache` en `SyncService` para admitir el tipo `CheckIn`, asegurando una gestión adecuada de auditorías offline.
* **Retención de datos:** Las consultas filtran registros con `CreatedAt >= UtcNow.AddYears(-3)` para garantizar una ventana de retención activa de 3 años.
* **Exportación:** Se introdujo el endpoint `/api/admin/access-logs/export` con soporte para formatos `csv` y `pdf` (completado solo `csv` por ahora).