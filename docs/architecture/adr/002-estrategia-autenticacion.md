# ADR-002: Authentication Strategy

**Status**: 🟢 Accepted
**Date**: 2026-06-09
**Deciders**: @gymflow-tech-lead

---

## Context

GymFlow Lite handles sensitive gym member data (PII, payment info, access logs). The app runs in a browser (PWA) and must support offline use. We need an authentication strategy that:

- Protects against **XSS** (cross-site scripting) — common in web apps.
- Protects against **CSRF** (cross-site request forgery) — critical for cookie-based auth.
- Supports **offline token refresh** gracefully (gym staff may lose connectivity mid-session).
- Enforces **RBAC** (role-based access control) with 5 distinct roles.
- Does not require a custom auth server (we want to ship fast).

---

## Decision

We use a **dual-token JWT + Refresh Token in HttpOnly Cookie** strategy:

1. **Access Token (JWT)**: short-lived (15 min), stored **in memory only** (JS variable, never in storage).
2. **Refresh Token**: long-lived (7 days), stored in an **HttpOnly, Secure, SameSite=Lax cookie**.
3. **RBAC**: 5 roles — `Owner`, `Admin`, `Receptionist`, `Trainer`, `Member`. Roles are embedded in the JWT claims.
4. **Token refresh**: handled transparently by an Axios interceptor that calls `/auth/refresh` when the access token expires.

### Storage rules

| Token | Where | Why |
|-------|-------|-----|
| Access Token (JWT) | In-memory JS variable | XSS cannot read it; cleared on page reload |
| Refresh Token | HttpOnly Cookie | JS cannot read it; sent automatically on `/auth/refresh` |

### Cookie configuration

```
HttpOnly: true
Secure: true (in production)
SameSite: Lax
Path: /auth
Max-Age: 7 days
```

### Authorization header on API calls

```
Authorization: Bearer {access_token}
```

---

## Options Considered

### Option 1: JWT in localStorage ❌

**Pros**:
- Simplest to implement.
- Survives page reloads without refresh.

**Cons**:
- **XSS-vulnerable**: any script injection (compromised npm package, malicious ad, etc.) can read the token and impersonate the user.
- This is the #1 cause of web app breaches; rejected.

### Option 2: JWT in non-HttpOnly cookie ❌

**Pros**:
- Sent automatically on every request.
- Survives page reloads.

**Cons**:
- **CSRF-vulnerable**: any malicious site can trigger a request to our API with the cookie attached.
- Requires implementing CSRF tokens (separate cookie + header pattern), which is error-prone.

### Option 3: JWT in memory + Refresh in HttpOnly Cookie ✅ (chosen)

**Pros**:
- Access token cannot be stolen via XSS (not in storage).
- Refresh token cannot be stolen via XSS (HttpOnly).
- CSRF is mitigated by SameSite=Lax (cookies are not sent on cross-site POSTs).
- The refresh flow is transparent to the user.

**Cons**:
- Access token is lost on page reload → must call `/auth/refresh` on app boot.
- More moving parts (two tokens, refresh interceptor) → more to test.

### Option 4: Server-side session with Redis

**Pros**:
- Tokens can be revoked server-side (better for "log out everywhere").
- Simple conceptually.

**Cons**:
- Requires a Redis lookup on every authenticated request (latency + cost).
- Does not work offline (the API is unreachable → no session validation).
- Conflicts with our offline-first architecture (RFC-001).

### Option 5: OAuth / Auth0 / Clerk (third-party)

**Pros**:
- Battle-tested.
- MFA, social login, etc. out of the box.

**Cons**:
- Cost at scale.
- Vendor lock-in.
- Overkill for a small gym product.

---

## Consequences

### Positive

- Strong defense against XSS and CSRF (the two main web attack vectors).
- Works with our offline-first architecture: access token validates API calls when online; refresh works when reconnecting.
- No third-party auth dependency.

### Negative

- Page reloads require a refresh call (slight UX cost; mitigated by making it transparent).
- Logout-everywhere requires a server-side token blacklist (not implemented yet; tracked as tech debt).
- Refresh token rotation: when a refresh token is used, we issue a new one and invalidate the old (not yet implemented; on the roadmap).

### Neutral

- The 15-minute access token lifetime is a tradeoff between security and refresh frequency.
- We commit to standard JWT libraries (`System.IdentityModel.Tokens.Jwt` on backend, `jwt-decode` on frontend).

---

## References

- [OWASP JWT Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/JSON_Web_Token_for_Java_Cheat_Sheet.html)
- [OWASP Cookie Security](https://owasp.org/www-community/HttpOnly)
- [Auth0: Where to Store JWTs](https://auth0.com/docs/secure/security-guidance/data-security/token-storage)
- ADR-001: Technology Stack (HTTP framework, frontend stack)
- RFC-001: Architecture Offline Sync (offline behavior, ClientGuid)
