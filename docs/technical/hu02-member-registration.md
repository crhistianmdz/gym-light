# HU-02 — Registro de Socio con Foto Obligatoria

## Resumen

Permite registrar nuevos socios con foto obligatoria en formato WebP. El sistema valida, comprime y almacena la foto antes de persistir el socio en base de datos.

## Endpoints

### POST /api/members

**Headers requeridos:**
- `X-Client-Guid: {uuid-v4}` — para idempotencia
- `Authorization: Bearer {token}` — roles: Receptionist, Admin
- `Content-Type: multipart/form-data`

**Body (form-data):**

| Campo       | Tipo   | Requerido | Validación                    |
|-------------|--------|-----------|-------------------------------|
| FirstName   | string | ✅        | 2–100 caracteres              |
| LastName    | string | ✅        | 2–100 caracteres              |
| Email       | string | ✅        | formato válido, único en DB   |
| PhoneNumber | string | ✅        | 7–20 caracteres               |
| Photo       | file   | ✅        | WebP, máx. 500KB              |

**Respuesta exitosa — 201 Created:**
```json
{
  "memberId": "550e8400-e29b-41d4-a716-446655440000",
  "fullName": "Juan Pérez",
  "email": "juan@example.com",
  "photoUrl": "/photos/550e8400-e29b-41d4-a716-446655440000.webp",
  "status": "Active",
  "createdAt": "2025-01-15T10:30:00Z"
}
```

**Errores posibles:**

| Código | Causa                          |
|--------|--------------------------------|
| 400    | Validación fallida             |
| 409    | Email ya registrado            |
| 422    | Foto no es WebP válido         |
| 500    | Error interno                  |

---

## Arquitectura Backend

### Flujo de datos

```
MembersController
  └── IdempotencyFilter (X-Client-Guid)
       └── RegisterMemberUseCase
             ├── CreateMemberValidator (FluentValidation)
             ├── IMemberRepository.GetByEmailAsync()  ← verifica unicidad
             ├── IPhotoStorageService.SaveAsync()     ← guarda WebP
             ├── Member.CreateWithId(preGeneratedId)  ← usa GUID pre-generado
             └── IMemberRepository.AddAsync()
```

### Por qué `Member.CreateWithId()` y no `Member.Create()`

El Use Case **pre-genera el GUID** antes de llamar a `IPhotoStorageService`, porque ese GUID se usa como nombre del archivo de foto (`{memberId}.webp`). Si se usara `Member.Create()` (que genera su propio GUID internamente), habría un mismatch entre el GUID del archivo de foto y el GUID de la entidad en base de datos.

### `Result<T>` pattern

No se lanzan excepciones como control de flujo. Todos los casos de error retornan `Result<T>` con tipo discriminado:

```csharp
Result<MemberDto>.Success(dto)
Result<MemberDto>.ValidationError("Email ya registrado")
Result<MemberDto>.InternalError("Error al guardar foto")
```

### `IPhotoStorageService`

Interfaz que abstrae el almacenamiento de fotos. Implementación actual: `LocalPhotoStorageService` (guarda en `wwwroot/photos/`). Diseñada para ser reemplazada por Azure Blob / S3 sin modificar el Use Case (DI).

---

## Arquitectura Frontend

### Flujo de registro

```
MemberForm (UI)
  └── useRegisterMember (hook)
        ├── imageService.compressToWebP()   ← Canvas API, sin libs externas
        ├── imageService.estimateSizeKB()   ← valida < 500KB post-compresión
        └── memberService.register()
              ├── fetch POST /api/members   ← multipart/form-data
              │     AbortController 2s timeout
              └── Si OK: memberService.hydrateMember()  ← guarda en IndexedDB
```

### Consideraciones importantes

**Safari y WebP:**
`canvas.toDataURL('image/webp')` en Safari retorna `data:image/png` porque Safari no soporta WebP encoding en Canvas (solo decoding). `compressToWebP()` detecta esto explícitamente y muestra un error al usuario en lugar de enviar una imagen PNG renombrada como WebP.

**Sin fallback offline para registro:**
A diferencia del check-in (HU-01), el registro de socio NO tiene fallback offline. Crear un socio sin conectividad generaría problemas de integridad de datos (duplicados potenciales al reconectar). Se requiere conexión obligatoria.

**IndexedDB post-registro:**
Una vez que el servidor confirma el registro (201), el frontend hidrata IndexedDB con los datos del socio para consultas offline futuras.

---

## Componentes

### `MemberForm`

Formulario principal. Gestiona:
- Campos del formulario con validación client-side
- Delegación de captura de foto a `PhotoCapture`
- Llamada a `useRegisterMember` en submit

### `PhotoCapture`

Sub-componente que maneja:
1. Captura de imagen (input file o cámara)
2. Preview de la imagen seleccionada
3. Compresión a WebP vía `imageService.compressToWebP()`
4. Validación de tamaño post-compresión

### `useRegisterMember`

Hook que encapsula:
- Estado del formulario (loading, error, success)
- Lógica de compresión + validación de foto
- Llamada a `memberService.register()`
- Hidratación de IndexedDB en éxito

---

## Reglas del negocio

1. La foto es **obligatoria** — no se puede registrar un socio sin foto
2. La foto debe ser **WebP** — el servidor rechaza otros formatos
3. El tamaño máximo post-compresión es **500KB**
4. El email debe ser **único** en el sistema
5. El `X-Client-Guid` es obligatorio — el `IdempotencyFilter` rechaza requests sin él
6. El registro requiere rol **Receptionist** o **Admin**