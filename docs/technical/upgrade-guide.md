# GymFlow Lite — Guía de Upgrade de Schema

> Guía paso a paso para realizar upgrades de schema entre versiones de GymFlow Lite.
> Este documento cubre el proceso completo: preparación, ejecución, verificación y rollback.

**Versión del documento**: 1.0
**Última actualización**: 2026-06-11
**Aplica a**: GymFlow Lite ≥ v1.0

---

## Índice

1. [Pre-requisitos](#1-pre-requisitos)
2. [Antes del Upgrade](#2-antes-del-upgrade)
3. [Durante el Upgrade](#3-durante-el-upgrade)
4. [Después del Upgrade](#4-después-del-upgrade)
5. [Rollback](#5-rollback-en-caso-de-fallo)
6. [Ejemplo Completo: v1.0 → v1.1](#6-ejemplo-completo-v10--v11)
7. [Preguntas Frecuentes](#7-preguntas-frecuentes)
8. [Referencias](#8-referencias)

---

## 1. Pre-requisitos

Antes de hacer un upgrade, asegurate de tener:

| Requisito | Cómo verificarlo |
|---|---|
| **Docker** corriendo (PostgreSQL + Redis) | `docker compose -f docker/docker-compose.yml ps` |
| **CLI de GymFlow** instalado | `gymflow-cli --version` (≥ 1.0.0) |
| **Credenciales de admin** | Necesarias para autenticación con la API |
| **Espacio en disco** | Mínimo 500 MB libres para el backup |
| **Acceso a `pg_dump`/`pg_restore`** | `which pg_dump` (incluido en el container PostgreSQL) |

### Configuración del CLI

```bash
# Variables de entorno requeridas
export GYMFLOW_API_URL=http://localhost:5000
export GYMFLOW_API_KEY=tu-api-key-aqui

# O configurar en el archivo de configuración
gymflow-cli config set api.url http://localhost:5000
gymflow-cli config set api.key tu-api-key-aqui
```

---

## 2. Antes del Upgrade

### 2.1 Verificar el estado actual

```bash
gymflow-cli status
```

Salida esperada:

```
Schema Version: 1.0.0
Last Migration: 20260611123000_AddSchemaVersionTable
Applied At: 2026-06-11 12:30:00 UTC
Pending Migrations: 3
Disk Space: 12.4 GB free
Modules:
  core      1.0.0
  plugins   1.0.0
```

### 2.2 Revisar qué cambia

Consultá el [CHANGELOG.md](../../CHANGELOG.md) para ver qué incluye la nueva versión.

### 2.3 Backup manual (Siempre recomendado)

Aunque el upgrader crea un backup automático, **siempre hacé un backup manual primero**:

```bash
# Desde el host
docker compose -f docker/docker-compose.yml exec postgres \
  pg_dump -U gymflow -d gymflow_dev --format=custom \
  > ~/backups/gymflow_pre_upgrade_$(date +%Y%m%d_%H%M%S).dump
```

O usando el CLI:

```bash
gymflow-cli backup create --output ~/backups/
```

### 2.4 Validar compatibilidad del schema

```bash
gymflow-cli doctor
```

Verificá que todos los checks pasen en verde:

```
✅ Docker running
✅ PostgreSQL 16: healthy
✅ Redis 7: healthy
✅ Schema consistency: OK
✅ Migration policy: compliant (0 violations)
✅ Disk space: 12.4 GB free
```

### 2.5 Checklist pre-upgrade

- [ ] Backup manual creado y verificado
- [ ] `gymflow-cli status` muestra el estado actual correcto
- [ ] `gymflow-cli doctor` pasa todos los checks
- [ ] CHANGELOG revisado — entendés qué cambia
- [ ] Nadie está usando el sistema activamente (ventana de mantenimiento)
- [ ] Espacio en disco suficiente (≥ 500 MB)

---

## 3. Durante el Upgrade

### 3.1 Dry run (simulación)

Siempre empezá con un dry run para ver qué migraciones se aplicarían:

```bash
gymflow-cli upgrade --target 1.1.0 --dry-run
```

Salida esperada:

```
[DRY RUN] Simulando upgrade de 1.0.0 → 1.1.0

Migraciones que se aplicarían:
  1. 20260611140000_AddMemberNotes.cs
     → Agrega columna notes a Members (policy: ✅ compliant)
  2. 20260611150000_AddPaymentIndex.cs
     → Crea índice en Payments.Timestamp (policy: ✅ compliant)
  3. 20260611160000_IncreaseProductNameLength.cs
     → Aumenta maxLength de Products.Name: 100 → 200 (policy: ✅ compliant)

Total: 3 migraciones pendientes
Espacio requerido: ~150 MB
Tiempo estimado: 45 segundos
```

### 3.2 Ejecutar el upgrade

```bash
gymflow-cli upgrade --target 1.1.0 --verbose
```

Salida esperada:

```
🔒 Adquiriendo lock de schema...
✅ Lock adquirido (ID: 1701)

📋 Pre-checks:
  ✅ Target version 1.1.0 es válida
  ✅ Versión actual: 1.0.0
  ✅ Espacio en disco: 12.3 GB (mínimo requerido: 500 MB)

💾 Creando backup...
✅ Backup creado: backup_20260611_130000.dump (45 MB)

📦 Aplicando migraciones...
  [1/3] 20260611140000_AddMemberNotes.cs
    ✅ Aplicada en 2.3s
  [2/3] 20260611150000_AddPaymentIndex.cs
    ✅ Aplicada en 1.1s
  [3/3] 20260611160000_IncreaseProductNameLength.cs
    ✅ Aplicada en 0.8s

🔓 Liberando lock...

✨ Upgrade completado exitosamente
  Versión anterior: 1.0.0
  Nueva versión:     1.1.0
  Migraciones:       3 aplicadas
  Duración total:    8.5 segundos
```

### 3.3 Opciones del comando `upgrade`

| Flag | Descripción |
|---|---|
| `--target <version>` | Versión de schema destino (obligatorio) |
| `--skip-backup` | No crear backup antes del upgrade (NO recomendado en producción) |
| `--dry-run` | Simular el upgrade sin aplicar cambios |
| `--verbose` | Mostrar salida detallada |
| `--force` | Forzar upgrade aunque la versión actual sea ≥ target |

---

## 4. Después del Upgrade

### 4.1 Verificar el nuevo estado

```bash
gymflow-cli status
```

Confirmá que la versión se actualizó:

```
Schema Version: 1.1.0        ← Nueva versión
Last Migration: 20260611160000_IncreaseProductNameLength
Applied At: 2026-06-11 13:00:00 UTC
Pending Migrations: 0        ← Sin pendientes
```

### 4.2 Ejecutar doctor de nuevo

```bash
gymflow-cli doctor
```

Verificá que todo siga en verde post-upgrade.

### 4.3 Checklist post-upgrade

- [ ] `gymflow-cli status` muestra la nueva versión
- [ ] `gymflow-cli doctor` pasa todos los checks
- [ ] La aplicación funciona correctamente (probar flujos principales)
- [ ] Los datos existentes están intactos (verificar algunos registros)
- [ ] El backup pre-upgrade se guarda en lugar seguro (por 30 días)

---

## 5. Rollback (en caso de fallo)

### 5.1 Rollback automático

Si una migración falla durante el upgrade, el sistema revierte automáticamente:

```
📦 Aplicando migraciones...
  [1/3] 20260611140000_AddMemberNotes.cs
    ✅ Aplicada en 2.3s
  [2/3] 20260611150000_AddPaymentIndex.cs
    ❌ ERROR: duplicate key value violates unique constraint

⏪ Iniciando rollback automático...
🔄 Restaurando desde backup: backup_20260611_130000.dump
✅ Rollback completado. Base de datos restaurada a versión 1.0.0.

❌ Upgrade falló: Migration 20260611150000_AddPaymentIndex.cs error
   Detalle: duplicate key value violates unique constraint "IX_Payments_ClientGuid"
   Recomendación: Revisar datos duplicados antes de reintentar.
```

### 5.2 Rollback manual (si falla el automático)

```bash
# Restaurar desde el backup manual
docker compose -f docker/docker-compose.yml exec -T postgres \
  pg_restore -U gymflow -d gymflow_dev --clean \
  < ~/backups/gymflow_pre_upgrade_20260611_130000.dump

# Verificar el estado después del restore
gymflow-cli status
```

### 5.3 Rollback a una versión específica

```bash
gymflow-cli rollback --target 1.0.0
```

---

## 6. Ejemplo Completo: v1.0 → v1.1

### Contexto

- Versión actual: **v1.0.0** (schema `__EFMigrationsHistory` + `schema_version`)
- Versión objetivo: **v1.1.0** (agrega `notes` a Members, índice en Payments, aumenta Name en Products)
- Entorno: **staging** (pre-producción)
- Ventana de mantenimiento: **2:00 AM - 2:30 AM UTC**

### Paso a paso

```bash
# ── 1. Verificar estado ──
$ gymflow-cli status
Schema Version: 1.0.0
...

# ── 2. Validar integridad ──
$ gymflow-cli doctor
✅ ...
✅ Migration policy: compliant (0 violations)

# ── 3. Backup manual ──
$ gymflow-cli backup create --output /backups/
Backup created: /backups/gymflow_v1.0.0_20260611_015500.dump (52 MB)

# ── 4. Dry run ──
$ gymflow-cli upgrade --target 1.1.0 --dry-run
[DRY RUN] 3 migraciones pendientes. 0 violaciones de política.

# ── 5. Upgrade real ──
$ gymflow-cli upgrade --target 1.1.0 --verbose
...
✨ Upgrade completado. Nueva versión: 1.1.0

# ── 6. Verificar ──
$ gymflow-cli status
Schema Version: 1.1.0 ✅
Pending Migrations: 0 ✅

$ gymflow-cli doctor
...
✅ Schema consistency: OK
✅ Migration policy: compliant

# ── 7. Smoke test ──
$ curl -s http://localhost:5000/api/health | jq .status
"healthy"
```

### Línea de tiempo

| Hora | Acción | Duración |
|---|---|---|
| 01:55 | Backup manual | 15s |
| 01:56 | Dry run | 2s |
| 02:00 | Inicio del upgrade | — |
| 02:00 | Lock + pre-checks | 1s |
| 02:00 | Backup automático | 12s |
| 02:00 | Migración 1/3 | 3s |
| 02:00 | Migración 2/3 | 2s |
| 02:00 | Migración 3/3 | 1s |
| 02:01 | Unlock + verificación | 1s |
| **Total** | | **~5 min** (incluyendo verificación) |

---

## 7. Preguntas Frecuentes

### ¿Qué pasa si el upgrade falla a mitad de camino?

El sistema hace rollback automático restaurando la base de datos desde el backup creado al inicio. Ninguna migración parcial queda aplicada.

### ¿Puedo saltarme versiones? (ej: v1.0 → v1.3)

Sí. El `SchemaUpgrader` aplica **todas** las migraciones pendientes entre la versión actual y la target, en orden semver. No es necesario aplicar versiones intermedias una por una.

### ¿Qué hago si el backup automático falla?

El upgrade se detiene antes de aplicar migraciones. Corregí el problema (espacio en disco, permisos, etc.) y reintentá.

### ¿Las migraciones son reversibles?

Las migraciones individuales no se revierten una a una. El rollback restaura la base completa desde el backup. El campo `RollbackSql` en `schema_version` permite documentar el SQL de reversión para migraciones complejas, pero no se ejecuta automáticamente.

### ¿Cuánto downtime implica un upgrade?

El tiempo que tardan en aplicarse las migraciones (típicamente 5-60 segundos). Durante ese lapso, el advisory lock impide que otros procesos modifiquen el schema. La aplicación puede seguir respondiendo lecturas, pero las escrituras que requieran las nuevas columnas pueden fallar momentáneamente.

### ¿Qué pasa si dos personas intentan hacer upgrade al mismo tiempo?

El advisory lock (ID 1701) garantiza que solo un proceso de upgrade puede ejecutarse a la vez. El segundo proceso recibe un error inmediato indicando que hay otro upgrade en progreso.

---

## 8. Referencias

- [Política de Schema Versioning](./schema-versioning.md) — Documentación completa del sistema
- [ADR-003: Estrategia de Migraciones](../architecture/adr/003-estrategia-migraciones.md)
- [ADR-007: Modelo Self-Hosted](../architecture/adr/007-modelo-self-hosted.md)
- [CHANGELOG.md](../../CHANGELOG.md)
- [Guía de Desarrollo Local](./local-setup.md)
