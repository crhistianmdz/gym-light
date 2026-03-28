# HU-05 — Autenticación Robusta y Sesión Offline

## Resumen

HU-05 implementa el sistema de autenticación completo de GymFlow Lite. Provee login con credenciales,
sesión persistente via refresh token en HttpOnly Cookie, renovación automática de tokens y logout seguro.
El access token (JWT) se guarda **exclusivamente en memoria** (nunca en localStorage). La sesión se
restaura automáticamente al recargar la app mediante un refresh silencioso en el mount.

---

## Endpoints

### POST /api/auth/login

**Headers:** `Content-Type: application/json`  
**Autorización:** ninguna (AllowAnonymous)

**Body:**
```json
{ "email": "recepcionista@gym.com", "password": "secreto" }
```

**Respuesta exitosa — 200 OK:**
```json
{
  "accessToken": "eyJhbGci...",
  "userId":      "550e8400-e29b-41d4-a716-446655440000",
  "fullName":    "Ana García",
  "role":        "Receptionist",
  "expiresAt":   "2025-01-15T10:45:00Z"
}
```
Además setea la cookie `gymflow_refresh` (HttpOnly, ver §Seguridad).

**Errores:**
| Código | Causa |
|--------|-------|
| 400    | Credenciales inválidas (usuario no encontrado, inactivo, o password incorrecto) |

---

### POST /api/auth/refresh

**Headers:** ninguno (el refresh token viene de la cookie automáticamente)  
**Autorización:** ninguna (AllowAnonymous)

**Respuesta exitosa — 200 OK:** mismo formato que login.  
Rota la cookie `gymflow_refresh` (token viejo revocado, token nuevo emitido).

**Errores:**
| Código | Causa |
|--------|-------|
| 401    | No hay cookie, token inválido o expirado |

---

### POST /api/auth/logout

**Autorización:** ninguna (AllowAnonymous — el cliente puede estar sin AT válido)

**Respuesta — 204 No Content.**  
Revoca el refresh token en DB y elimina la cookie `gymflow_refresh`.

---

## Arquitectura Backend

### Flujo completo de login

```
Cliente
  └── POST /api/auth/login { email, password }
        └── AuthController.Login()
              └── LoginUseCase.ExecuteAsync()
                    ├── IUserRepository.GetByEmailAsync()   ← busca por email (lowercased)
                    ├── user.IsActive check
                    ├── IPasswordHasher.Verify(password, user.PasswordHash)  ← BCrypt workFactor=12
                    ├── ITokenService.GenerateAccessToken(user)   ← JWT 15min, claims: sub/email/role/jti
                    ├── ITokenService.GenerateRefreshToken()      ← 64 bytes random → base64
                    ├── SHA256(rawToken) → tokenHash
                    ├── RefreshToken.Create(userId, tokenHash, expiresAt=+7d)
                    ├── IRefreshTokenRepository.AddAsync()
                    └── Result<AuthResponseDto>.SuccessWithExtra(dto, rawToken)
              └── AuthController: SetRefreshCookie(result.Extra)
              └── return 200 OK { dto }
```

### Flujo de refresh (rotación de tokens)

```
Cliente (cookie gymflow_refresh automática)
  └── POST /api/auth/refresh
        └── AuthController.Refresh()
              ├── Lee cookie "gymflow_refresh" → rawToken
              └── RefreshTokenUseCase.ExecuteAsync(rawToken)
                    ├── SHA256(rawToken) → tokenHash
                    ├── IRefreshTokenRepository.GetByTokenHashAsync(tokenHash)
                    ├── storedToken.IsActive() → RevokedAt is null && now < ExpiresAt
                    ├── IUserRepository.GetByIdAsync(storedToken.UserId)
                    ├── IRefreshTokenRepository.RevokeAsync(storedToken)   ← ROTA: revoca viejo
                    ├── Genera nuevo AT + nuevo RT
                    ├── Guarda nuevo tokenHash en DB
                    └── Result<AuthResponseDto>.SuccessWithExtra(dto, newRawToken)
              └── SetRefreshCookie(result.Extra)   ← sobrescribe cookie con nuevo token
```

### Por qué `Result<T>.SuccessWithExtra`

Los Use Cases siguen el patrón `Result<T>` — no lanzan excepciones como control de flujo.
El raw refresh token necesita llegar al Controller para ser seteado en la cookie, pero NO debe
formar parte del DTO (que va al body de la respuesta). Se extendió `Result<T>` con:

```csharp
public string? Extra { get; private set; }
public static Result<T> SuccessWithExtra(T value, string extra) =>
    new() { IsSuccess = true, Value = value, Extra = extra };
```

El Controller accede via `result.Extra` y el cliente nunca ve el raw token en el body.

### Por qué SHA256 en DB y no el token raw

Si la DB es comprometida, el atacante obtiene hashes, no tokens válidos.
El raw token solo existe en dos lugares: la memoria del Use Case (efímera) y la HttpOnly Cookie del cliente.
El flujo de verificación es: `SHA256(rawToken_de_cookie) == tokenHash_en_DB`.

---

## Seguridad

### Configuración de la cookie

| Atributo   | Valor          | Por qué |
|------------|----------------|---------|
| Name       | `gymflow_refresh` | nombre fijo, leído en AuthController |
| HttpOnly   | `true`         | JavaScript no puede acceder — protege contra XSS |
| Secure     | `true`         | Solo se envía por HTTPS |
| SameSite   | `Strict`       | No se envía en requests cross-site — protege contra CSRF |
| Path       | `/api/auth`    | Solo se adjunta a los endpoints de auth, no a toda la API |
| Expires    | `+7 días`      | Alineado con expiración del RefreshToken en DB |

### Rotación de tokens

En cada refresh se genera un par nuevo (AT + RT). El RT viejo es revocado inmediatamente antes
de emitir el nuevo. Si un atacante roba un RT y lo usa, el token legítimo del usuario fallará
en su próximo intento y detectará la actividad sospechosa.

### Access Token en memoria

El AT se guarda en `tokenRef` (`useRef`) en el `AuthContext` del frontend — no en `localStorage`
ni `sessionStorage`. Esto protege contra ataques XSS que intentan extraer tokens del storage.
Trade-off: se pierde al recargar la página, pero el silent refresh on mount lo restaura
transparentemente desde la HttpOnly Cookie.

---

## Arquitectura Frontend

### Flujo de inicialización

```
App mount
  └── AuthProvider (useEffect on mount)
        └── apiRefresh()   ← silent refresh — intenta restaurar sesión desde cookie
              ├── éxito → tokenRef.current = newAT, setUser(session)
              └── error  → setUser(null), isLoading=false   (no hay sesión activa)
        └── initHttpClient(getToken, doRefresh, doLogout)   ← wires singleton
```

### Flujo de login

```
LoginForm.handleSubmit()
  └── useAuth().login({ email, password })
        └── apiLogin(payload)   ← POST /api/auth/login, credentials:'include'
              └── éxito → tokenRef.current = AT, setUser(session)
```

### fetchWithAuth — cliente HTTP autenticado

```
fetchWithAuth(url, init)
  ├── headers.set('Authorization', `Bearer ${tokenRef.current}`)
  ├── fetch(url, { ...init, headers, credentials:'include' })
  ├── si res.status === 401
  │     ├── doRefresh()   ← POST /api/auth/refresh (automático via cookie)
  │     │     └── éxito → tokenRef actualizado
  │     ├── retry fetch con nuevo token
  │     └── si refresh falla → doLogout() + throw Error
  └── return response
```

El `httpClient` es un módulo singleton. Se inicializa una sola vez en el `AuthProvider` via
`initHttpClient(getToken, refresh, logout)`. Los servicios (`accessService`, `memberService`)
deben usar `fetchWithAuth` en lugar de `fetch` raw para endpoints autenticados.

### Estado de AuthContext

| Campo           | Tipo              | Descripción |
|-----------------|-------------------|-------------|
| `user`          | `AuthSession\|null` | null si no autenticado |
| `isAuthenticated`| `boolean`        | `user !== null` |
| `isLoading`     | `boolean`        | true durante el silent refresh inicial |
| `login()`       | función           | llama apiLogin, setea tokenRef y user |
| `logout()`      | función           | llama apiLogout, limpia tokenRef y user |

---

## ConnectedCheckInPanel

El `CheckInPanel` original recibe `currentUserId` como prop explícita (bueno para testing).
`ConnectedCheckInPanel` es un wrapper que inyecta ese valor desde `useAuth()`:

```tsx
export function ConnectedCheckInPanel() {
  const { user } = useAuth()
  return <CheckInPanel currentUserId={user?.userId ?? ''} />
}
```

Usar `ConnectedCheckInPanel` en la app. Usar `CheckInPanel` directamente solo en tests unitarios.

---

## Configuración requerida (appsettings)

```json
{
  "Jwt": {
    "Secret":   "min-32-chars-secret-key-here",
    "Issuer":   "gymflow",
    "Audience": "gymflow-client"
  }
}
```

`Jwt:Secret` debe tener al menos 32 caracteres para HS256. En producción usar un secret manager
(Azure Key Vault, AWS Secrets Manager) — nunca commitear el valor real.

---

## Reglas de negocio

1. El access token dura **15 minutos**. Se guarda en memoria únicamente.
2. El refresh token dura **7 días**. Se guarda como SHA256 hex en DB. Raw token solo en cookie.
3. **Rotación obligatoria**: cada refresh revoca el token anterior y emite uno nuevo.
4. Un usuario inactivo (`IsActive = false`) no puede autenticarse aunque las credenciales sean correctas.
5. El email se normaliza a lowercase en `AppUser.Create()` y en la búsqueda del repositorio.
6. La cookie `gymflow_refresh` tiene `Path=/api/auth` — no se envía a `/api/members` ni `/api/access`.
7. El logout revoca el RT en DB y limpia la cookie. Si el cliente no tiene cookie (ya expiró), el logout igual devuelve 204.
8. `fetchWithAuth` reintenta **una sola vez** tras un 401. Si el segundo intento también falla, llama a `logout()` y lanza un error.
9. Prohibido guardar el access token en `localStorage` o `sessionStorage`.
