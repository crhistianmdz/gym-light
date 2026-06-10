# API-XXX: [Nombre del Endpoint]

**Status**: 🟡 Draft | 🟢 Implemented
**Version**: v1
**Created**: YYYY-MM-DD

---

## 🔌 Información General

- **Método HTTP**: `GET | POST | PUT | PATCH | DELETE`
- **Path**: `/api/v1/recurso/accion`
- **Auth requerida**: Sí | No
- **Roles permitidos**: `Owner | Admin | Receptionist | Trainer | Member`
- **Rate limit**: X requests / Y minutos (si aplica)

---

## 📥 Request

### Headers

```
Authorization: Bearer {token}
Content-Type: application/json
```

### Path Parameters

| Nombre | Tipo | Descripción |
|--------|------|-------------|
| `id` | UUID | ID del recurso |

### Query Parameters

| Nombre | Tipo | Required | Default | Descripción |
|--------|------|----------|---------|-------------|
| `page` | int | No | 1 | Número de página |
| `size` | int | No | 20 | Tamaño de página |

### Body (si aplica)

```json
{
  "campo1": "string",
  "campo2": 123,
  "campo3": true
}
```

---

## 📤 Response

### 200 OK (o 201 Created)

```json
{
  "id": "uuid",
  "campo1": "valor",
  "campo2": 123,
  "createdAt": "2026-06-09T12:00:00Z"
}
```

### Errores Posibles

| Código | Causa | Body |
|--------|-------|------|
| 400 | Bad Request | `{"error": "Mensaje claro"}` |
| 401 | No autenticado | `{"error": "Token inválido"}` |
| 403 | Sin permisos | `{"error": "Rol insuficiente"}` |
| 404 | No encontrado | `{"error": "Recurso no existe"}` |
| 409 | Conflicto | `{"error": "Recurso ya existe"}` |
| 500 | Server error | `{"error": "Internal error"}` |

---

## 📋 Reglas de Negocio

- **RN-1**: [Regla específica que aplica]
- **RN-2**: [Otra regla]

---

## 🔄 Offline / Sync (si aplica)

- ¿Soporta offline? Sí | No
- **ClientGuid**: Sí | No
- **Idempotency**: Sí | No
- **Conflicto strategy**: Server wins | Client wins | Manual

---

## 🧪 Ejemplos

### cURL

```bash
curl -X POST https://api.example.com/api/v1/recurso \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{"campo1": "valor", "campo2": 123}'
```

### Response ejemplo

```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "campo1": "valor",
  "campo2": 123,
  "createdAt": "2026-06-09T12:00:00Z"
}
```

---

## 🔗 Referencias

- HU-XXX que motiva este endpoint
- ADR-XXX (si hay decisión arquitectónica)
- PR-XXX que lo implementa
