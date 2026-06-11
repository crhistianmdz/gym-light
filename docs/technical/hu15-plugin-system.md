# HU-015 — Sistema de Plugins

## Overview

**Purpose:** HU-015 implementa un sistema de plugins modular que permite extender la funcionalidad de GymFlow Lite sin modificar el código core. Los plugins son assemblies .NET que implementan la interfaz `IPlugin` y pueden ser cargados dinámicamente.

**RBAC:**
- `Admin`/`Owner`: Puede gestionar plugins (listar, habilitar, deshabilitar).
- `Member`: Sin acceso a gestión de plugins.

---

## Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                         GymFlow Lite                                │
├─────────────────────────────────────────────────────────────────────┤
│  Startup                                                            │
│    ├─ PluginLoader.DiscoverAsync() → carga *.dll desde /plugins   │
│    ├─ valida cada plugin (IPlugin)                                 │
│    ├─ registra servicios via ConfigureServices()                   │
│    └─ sincroniza con PluginRegistry (DB)                          │
│                                                                     │
│  Request Flow                                                       │
│    ┌──────────┐    ┌──────────────┐    ┌───────────────────┐     │
│    │ HTTP     │───▶│ PluginsController │───▶│ PluginRegistry    │     │
│    │ Request  │    │ (GET/PATCH)    │    │ (Entity + Repo)   │     │
│    └──────────┘    └──────────────┘    └───────────────────┘     │
└─────────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────────┐
│  Plugins Directory (src/backend/Plugins/)                          │
├─────────────────────────────────────────────────────────────────────┤
│  GymFlow.Plugins.Abstractions/                                     │
│    ├─ IPlugin.cs (interface)                                       │
│    └─ PluginMetadata.cs (record)                                   │
│                                                                     │
│  Anthropometry/  → AnthropometryPlugin.cs                        │
│  Routines/      → RoutinesPlugin.cs                             │
│  Sales/         → SalesPlugin.cs                               │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Domain Model

### PluginRegistry (Entity)

Entidad que persiste el estado de cada plugin instalado.

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Id` | `string` | Identificador único del plugin |
| `Name` | `string` | Nombre para display |
| `Version` | `string` | Versión semver (ej: "1.0.0") |
| `Enabled` | `bool` | Si el plugin está activo |
| `OfflineCapable` | `bool` | Si soporta modo offline |
| `InstalledAt` | `DateTime` | Fecha de instalación |
| `LastUpdated` | `DateTime` | Última modificación |

---

## IPlugin Interface

```csharp
// src/backend/Plugins/GymFlow.Plugins.Abstractions/IPlugin.cs

using Microsoft.Extensions.DependencyInjection;

public interface IPlugin
{
    PluginMetadata Metadata { get; }
    void ConfigureServices(IServiceCollection services);
}
```

```csharp
// src/backend/Plugins/GymFlow.Plugins.Abstractions/PluginMetadata.cs

public record PluginMetadata(
    string Id,
    string Name,
    string Version,
    bool OfflineCapable);
```

### Implementación básica

```csharp
using GymFlow.Plugins.Abstractions;
using Microsoft.Extensions.DependencyInjection;

public class MyPlugin : IPlugin
{
    public PluginMetadata Metadata => new(
        Id: "my-plugin",
        Name: "My Plugin",
        Version: "1.0.0",
        OfflineCapable: false);

    public void ConfigureServices(IServiceCollection services)
    {
        // Registrar servicios del plugin
        // services.AddScoped<IMyService, MyService>();
    }
}
```

---

## PluginLoader Mechanism

```csharp
// src/backend/WebAPI/Plugins/PluginLoader.cs

public class DiscoveredPlugin
{
    public required IPlugin Instance { get; init; }
    public required PluginMetadata Metadata { get; init; }
    public required string AssemblyPath { get; init; }
}

public interface IPluginLoader
{
    Task<IEnumerable<DiscoveredPlugin>> DiscoverAsync(string pluginsPath);
    void ValidatePlugin(IPlugin plugin);
    void RegisterServices(IServiceCollection services, IPlugin plugin);
}
```

### Flujo de carga

1. **DiscoverAsync**: Escanea el directorio de plugins buscando `*.dll`
2. **LoadAssembly**: Carga cada DLL en memoria via `Assembly.Load(bytes)`
3. **Find Types**: Busca tipos que implementen `IPlugin`
4. **ValidatePlugin**: Verifica que `Metadata.Id`, `Name`, y `Version` estén presentes
5. **RegisterServices**: Llama a `ConfigureServices()` para registrar servicios en DI
6. **Sync to DB**: Sincroniza con `PluginRegistry` en la base de datos

---

## Admin API Endpoints

| Método | Endpoint | Descripción | Rol |
|--------|----------|------------|-----|
| `GET` | `/api/plugins` | Listar todos los plugins | Admin, Owner |
| `GET` | `/api/plugins/{id}` | Obtener plugin por ID | Admin, Owner |
| `PATCH` | `/api/plugins/{id}/enable` | Habilitar plugin | Admin, Owner |
| `PATCH` | `/api/plugins/{id}/disable` | Deshabilitar plugin | Admin, Owner |

### Response DTO

```csharp
public record PluginResponseDto(
    string Id,
    string Name,
    string Version,
    bool Enabled,
    bool OfflineCapable,
    DateTime InstalledAt,
    DateTime LastUpdated);
```

---

## Frontend Service

```typescript
// src/frontend/src/services/pluginService.ts

export interface PluginResponse {
  id: string;
  name: string;
  version: string;
  enabled: boolean;
  offlineCapable: boolean;
  installedAt: string;
  lastUpdated: string;
}

export async function getPlugins(): Promise<PluginResponse[]> {
  const response = await fetchWithAuth(`${API_BASE}/api/plugins`, {
    method: 'GET',
  });
  return response.json();
}

export async function enablePlugin(id: string): Promise<PluginResponse> {
  const response = await fetchWithAuth(`${API_BASE}/api/plugins/${id}/enable`, {
    method: 'PATCH',
  });
  return response.json();
}

export async function disablePlugin(id: string): Promise<PluginResponse> {
  const response = await fetchWithAuth(`${API_BASE}/api/plugins/${id}/disable`, {
    method: 'PATCH',
  });
  return response.json();
}
```

---

## Folder Structure

```
src/backend/Plugins/
├── GymFlow.Plugins.Abstractions/
│   ├── IPlugin.cs
│   └── PluginMetadata.cs
├── Anthropometry/
│   └── AnthropometryPlugin.cs
├── Routines/
│   └── RoutinesPlugin.cs
└── Sales/
    └── SalesPlugin.cs
```

---

## Business Rules

1. Los plugins se cargan desde el directorio configurado en `appsettings.json`
2. Un plugin puede registrar servicios adicionales en DI via `ConfigureServices()`
3. El estado habilitado/deshabilitado se persiste en la base de datos (`PluginRegistry`)
4. La carga de plugins ocurre al startup de la aplicación
5. Plugins deshabilitados no tienen servicios registrados en DI
6. No hay soporte para hot-reload en v1 (requiere restart)
7. Plugins de terceros deben ser copiados manualmente al directorio de plugins