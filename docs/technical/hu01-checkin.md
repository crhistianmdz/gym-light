# Documentación Técnica: HU-01 Check-in

## **Resumen**
La Historia de Usuario HU-01 "Check-in" implementa el flujo para registrar la entrada de socios al gimnasio mediante una estrategia "Network-First con fallback offline" que utiliza IndexedDB local en caso de fallos de conexión. Se asegura la idempotencia del registro mediante el uso de un `ClientGuid`, garantizando que solicitudes duplicadas no generen lógica redundante.

---

## **Endpoint**
- **Ruta:** `POST /api/access/checkin`
- **Rol Autorizado:** `Receptionist, Admin`
- **Request:**
  ```json
  {
    "memberId": "GUID del socio",
    "clientGuid": "UUID v4 generado en el cliente (cabecera: X-Client-Guid)",
    "performedByUserId": "GUID del usuario autenticado"
  }
  ```
- **Response:**
  - **200 OK:** Check-in permitido o duplicado detectado
    ```json
    {
      "allowed": true,
      "denialReason": null,
      "member": {
        "id": "GUID",
        "fullName": "string",
        "photoWebPUrl": "string",
        "status": "Active|Frozen|Expired",
        "membershipEndDate": "YYYY-MM-DD"
      }
    }
    ```
  - **403 Forbidden:** Acceso denegado
    ```json
    {
      "allowed": false,
      "denialReason": "string",
      "member": { null }
    }
    ```
  - **404 Not Found:** Socio no encontrado
    ```json
    {
      "title": "Socio no encontrado",
      "status": 404
    }
    ```

---

## **Arquitectura Backend**
```plaintext
AccessController
    → ValidateAccessUseCase
        → IAccessLogRepository
        → IMemberRepository
```
1. **Flujo de validación:**
    1. IDEMPOTENCIA: Verifica si el `ClientGuid` ya existe en la base de datos (AccessLog).
    2. BUSQUEDA: Recupera el socio por `MemberId`. Devuelve `404` si no existe.
    3. VALIDACIÓN: Llama `Member.CanAccess()` (rechaza si `status === Frozen|Expired`).
    4. REGISTRO: Crea un nuevo `AccessLog` con el resultado y lo persiste.

2. **Idempotencia**:
   - El filtro `IdempotencyFilter` verifica el header `X-Client-Guid`. Si el GUID existe en `IAccessLogRepository`, responde inmediatamente con `200 OK`.
3. **Persistencia (AccessLog):**
   ```json
   {
     "MemberId": "GUID",
     "ClientGuid": "UUID v4",
     "PerformedByUserId": "GUID",
     "WasAllowed": true|false,
     "Timestamp": "instant UTC",
     "IsOffline": false
   }
   ```

---

## **Estrategia Network-First con Fallback Offline**
1. **Timeout:**
   - Implementado en `accessService.fetchWithTimeout()` usando `AbortController`.
   - Si la API no responde en **2 segundos**, activa el fallback.
2. **Funcionamiento Offline:**
   - Busca en IndexedDB (`db.users`): valida **status** y fecha de expiración local.
   - Encola un `SyncQueueItem` en `sync_queue` con el `ClientGuid`.

---

## **Arquitectura Frontend**
1. **Flujo:**
   - `CheckInPanel` → `useCheckIn` → `checkInMember` → Red
   - Criterios de aceptación:
     - **CA-1:** Fallback IndexedDB.
     - **CA-2:** Foto WebP para verificación visual.
     - **CA-3:** Enqueue AccessLog.

2. **Estado:**
   ```plaintext
   idle → loading → allowed | denied | error
   ```

3. **ConnectedCheckInPanel:**
   - Wrapper que inyecta `currentUserId` desde `useAuth()` al `CheckInPanel` base.
   - Elimina el prop drilling del userId. Usar `ConnectedCheckInPanel` en la app; `CheckInPanel` queda disponible para tests con prop explícita.

---

## **Componentes**
1. **CheckInPanel:**
   - Permite enviar un `MemberId` para registrar el check-in.
   - Muestra un `MemberAccessCard` para resultados y estado del sistema (`SyncStatusBadge`).
   - Valida UI contra `loading|error`.

2. **MemberAccessCard:**
   - Frontera verde/roja según permiso de acceso (`allowed`).
   - Foto WebP + motivo de denegación (opcional).

3. **SyncStatusBadge:**
   - Indica sincronización:
     - Verde: Sincronizado.
     - Naranja: Pendientes.
     - Gris: Offline.

---

## **Dexie.js / IndexedDB**
- **Stores:**
  ```plaintext
  users:      'id, status, membershipEndDate'
  sync_queue: 'guid, type, timestamp'
  metadata:   'key'
  ```
- **Uso:**
  - `db.users:` Cachea perfiles de socio con fotos.
  - `db.sync_queue:` Registro temporal de operaciones offline.

---

## **Reglas de Negocio**
- **Acceso Denegado:**
  - Miembro `status === Frozen | Expired`.
  - Membresía vencida (`membershipEndDate < today`).
- **Idempotencia:**
  - GUID v4 asegura que la operación sea única por cliente.
- **Foto del socio:**
  - El check-in MUESTRA la foto WebP del socio en el `MemberAccessCard` para verificación visual.
  - La foto NO se captura en el check-in — fue registrada en HU-02. Si el socio no tiene foto aún, el campo `photoWebPUrl` puede estar vacío.
- **Desempeño:** Timeout de **2s** máximo para determinar fallback offline.