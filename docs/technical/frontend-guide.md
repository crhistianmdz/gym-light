# Frontend Guide — GymFlow Lite

## Estructura de carpetas

```
src/frontend/src/
├── components/
│   ├── CheckInPanel/         # HU-01: Panel de control de acceso
│   │   ├── CheckInPanel.tsx  # Componente principal — formulario + resultado
│   │   ├── MemberAccessCard.tsx  # Foto + estado visual (verde/rojo)
│   │   ├── SyncStatusBadge.tsx   # Indicador Verde/Naranja/Gris (tiempo real)
│   │   └── index.ts
│   └── MemberForm/           # HU-02: Formulario de registro de socio
│       ├── MemberForm.tsx    # Formulario completo
│       ├── PhotoCapture.tsx  # Selector + compresión WebP + preview
│       ├── MemberForm.types.ts
│       └── index.ts
├── db/
│   └── gymflow.db.ts         # Esquema Dexie + initDatabase()
├── hooks/
│   ├── useCheckIn.ts         # Estado de UI para check-in (HU-01)
│   └── useRegisterMember.ts  # Estado de UI para registro (HU-02)
├── services/
│   ├── accessService.ts      # Network-First + fallback offline (HU-01)
│   ├── imageService.ts       # Compresión WebP con Canvas API (HU-02)
│   └── memberService.ts      # Registro + hidratación IndexedDB (HU-02)
└── types/
    └── member.ts             # MemberDto, LocalMember
```

---

## Esquema Dexie

```typescript
import Dexie, { type EntityTable } from 'dexie'

const db = new GymFlowDatabase() // gymflow.db.ts

// Stores disponibles:
db.users       // EntityTable<LocalMember, 'id'>
db.sync_queue  // EntityTable<SyncQueueItem, 'guid'>
db.metadata    // EntityTable<LocalMetadata, 'key'>
```

### Tipos TypeScript

```typescript
type MemberStatus = 'Active' | 'Frozen' | 'Expired'
type SyncEventType = 'CheckIn' | 'Sale' | 'HealthUpdate'

interface LocalMember {
  id: string
  fullName: string
  photoWebP: string           // data URI WebP o URL relativa
  status: MemberStatus
  membershipEndDate: string   // 'YYYY-MM-DD'
}

interface SyncQueueItem {
  guid: string                // UUID v4 — ClientGuid
  type: SyncEventType
  payload: string             // JSON serializado
  timestamp: number           // Unix ms
  isOffline: boolean
  retryCount: number          // >= 3 → bandeja de errores
}
```

### Inicialización obligatoria

Llamar en el entry point de la app (`main.tsx`):

```typescript
import { initDatabase } from '@/db/gymflow.db'

await initDatabase() // solicita navigator.storage.persist() + abre la DB
```

---

## Hook: `useCheckIn`

```typescript
import { useCheckIn } from '@/hooks/useCheckIn'

function ReceptionPage({ currentUserId }: { currentUserId: string }) {
  const { checkIn, status, result, loading, error, reset } = useCheckIn()

  // status: 'idle' | 'loading' | 'allowed' | 'denied' | 'error'

  const handleCheckIn = () => checkIn(memberId, currentUserId)

  return (
    <>
      <button onClick={handleCheckIn} disabled={loading}>
        {loading ? 'Validando...' : 'Registrar acceso'}
      </button>

      {result && (
        <p>
          {result.allowed ? 'PERMITIDO' : 'DENEGADO'} —
          fuente: {result.source}  {/* 'online' | 'offline' */}
        </p>
      )}
    </>
  )
}
```

---

## Componente: `CheckInPanel`

```typescript
import { CheckInPanel } from '@/components/CheckInPanel'

// currentUserId viene del contexto de autenticación (HU-05)
<CheckInPanel currentUserId={authContext.userId} />
```

Incluye `SyncStatusBadge` integrado. No requiere props adicionales.

---

## Hook: `useRegisterMember`

```typescript
import { useRegisterMember } from '@/hooks/useRegisterMember'

function RegisterPage() {
  const { register, status, result, loading, error } = useRegisterMember()

  // status: 'idle' | 'compressing' | 'uploading' | 'success' | 'error'
  // 'compressing' → Canvas está procesando la imagen
  // 'uploading'   → fetch en curso

  return <span>{loading ? status : 'Listo'}</span>
}
```

---

## Componente: `MemberForm`

```typescript
import { MemberForm } from '@/components/MemberForm'

<MemberForm
  onSuccess={(memberId, fullName) => {
    console.log(`Socio ${fullName} (${memberId}) registrado`)
    navigate('/members')
  }}
/>
```

El botón submit permanece deshabilitado hasta que `PhotoCapture` confirme una foto válida. Esto implementa HU-02 CA-1.

---

## Flujo de `imageService.compressToWebP`

```
File (jpg/png/webp)
  │
  ├─ URL.createObjectURL(file)
  ├─ new Image() → img.onload
  ├─ Calcular dimensiones (máx 400px, aspect ratio preservado)
  ├─ canvas.drawImage(img, 0, 0, width, height)
  ├─ canvas.toDataURL('image/webp', 0.85)
  ├─ Verificar prefijo 'data:image/webp' (detecta Safari sin soporte)
  └─ Retornar data URI WebP
```

Sin dependencias externas. Solo Canvas API nativa del browser.

---

## Reglas que no se pueden romper

| Regla | Consecuencia de violarla |
|---|---|
| ❌ NUNCA `localStorage` para datos de socios | Pérdida de datos en limpieza automática del browser |
| ❌ NUNCA enviar imagen en formato distinto a WebP | El backend rechaza con 400 |
| ❌ NUNCA limpiar `sync_queue` sin confirmación del servidor | Pérdida permanente de transacciones offline |
| ❌ NUNCA cachear datos en IndexedDB sin `navigator.storage.persist()` | IndexedDB puede ser eliminada por el browser |
| ❌ NUNCA exponer el JWT en `localStorage` o `sessionStorage` | Vulnerable a XSS |
| ❌ NUNCA omitir `clientGuid` en una transacción de escritura | Duplicados en la sincronización |
| ❌ NUNCA permitir check-in si `status !== 'Active'` | Violación de regla de negocio PRD §4.1 |

---

## Variables de entorno

```bash
# .env.local
VITE_API_BASE_URL=https://localhost:5001
```

Si no está definida, `accessService.ts` y `memberService.ts` usan `''` (mismo origen).
