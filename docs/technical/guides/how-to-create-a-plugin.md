# Cómo Crear un Plugin para GymFlow Lite

Esta guía te muestra cómo crear un plugin desde cero. Un plugin es una DLL .NET que implementa la interfaz `IPlugin` y extiende la funcionalidad de GymFlow.

**Tiempo estimado:** ~5 minutos para un plugin básico.

---

## Requisitos Previos

- .NET 8 SDK instalado
- Referencias a `GymFlow.Plugins.Abstractions` (proyecto local o NuGet futuro)
- Conocimiento básico de C# y ASP.NET Core DI

---

## Quickstart (5 minutos)

### 1. Crear el proyecto

```bash
dotnet new classlib -n MyPlugin -o src/backend/Plugins/MyPlugin
cd src/backend/Plugins/MyPlugin
dotnet add reference ../GymFlow.Plugins.Abstractions/GymFlow.Plugins.Abstractions.csproj
```

### 2. Implementar IPlugin

Reemplaza `Class1.cs` con:

```csharp
using GymFlow.Plugins.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace GymFlow.Plugins.MyPlugin;

public class MyPlugin : IPlugin
{
    public PluginMetadata Metadata => new(
        Id: "my-plugin",
        Name: "My Plugin",
        Version: "1.0.0",
        OfflineCapable: false);

    public void ConfigureServices(IServiceCollection services)
    {
        // Registra servicios aquí si los necesitas
    }
}
```

### 3. Compilar y copiar

```bash
dotnet build
cp bin/Debug/net8.0/GymFlow.Plugins.MyPlugin.dll /ruta/a/plugins/
```

### 4. Reiniciar GymFlow

El plugin se descubre automáticamente al startup.

---

## Estructura del Proyecto

```
MyPlugin/
├── MyPlugin.csproj
├── MyPlugin.cs              ← Implementación de IPlugin
├── plugin.json             ← Manifiesto (opcional, para metadata extendida)
└── Services/
    └── MyService.cs       ← Servicios del plugin
```

---

## Referencia: IPlugin Interface

```csharp
namespace GymFlow.Plugins.Abstractions;

public interface IPlugin
{
    PluginMetadata Metadata { get; }
    void ConfigureServices(IServiceCollection services);
}
```

| Miembro | Descripción |
|--------|-------------|
| `Metadata` | Información del plugin (Id, Name, Version, OfflineCapable) |
| `ConfigureServices` | Punto de extensión para registrar servicios en DI |

---

## Referencia: PluginMetadata

```csharp
namespace GymFlow.Plugins.Abstractions;

public record PluginMetadata(
    string Id,           // Unique identifier (kebab-case)
    string Name,         // Display name
    string Version,      // Semver (ej: "1.0.0")
    bool OfflineCapable); // Soporta modo offline
```

### Validaciones

- `Id`: Requerido, no vacío
- `Name`: Requerido, no vacío
- `Version`: Requerido, no vacío
- `OfflineCapable`: Opcional, default `false`

---

## plugin.json (Manifiesto)

Formato opcional para metadata extendida:

```json
{
  "id": "my-plugin",
  "name": "My Plugin",
  "version": "1.0.0",
  "description": "Descripción del plugin",
  "author": "Tu Nombre",
  "offlineCapable": false,
  "dependencies": []
}
```

**Nota:** En v1, este archivo es informativo. La metadata real viene de `PluginMetadata`.

---

## Cómo Registrar Servicios en DI

El método `ConfigureServices` te permite registrar servicios en el contenedor de dependencias:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // Singleton
    services.AddSingleton<IMyService, MyService>();

    // Scoped
    services.AddScoped<IMyRepository, MyRepository>();

    // Transient
    services.AddTransient<IMyValidator, MyValidator>();

    // Options
    services.Configure<MyOptions>(options =>
    {
        options.Setting = "value";
    });
}
```

### Acceso a servicios existentes

Puedes inyectar servicios core de GymFlow:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // GymFlow usa estos servicios internamente
    // No necesitas registrarlos de nuevo
}
```

---

## Ejemplo: Hello World Plugin

```csharp
using GymFlow.Plugins.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace GymFlow.Plugins.HelloWorld;

public class HelloWorldPlugin : IPlugin
{
    public PluginMetadata Metadata => new(
        Id: "hello-world",
        Name: "Hello World",
        Version: "1.0.0",
        OfflineCapable: false);

    public void ConfigureServices(IServiceCollection services)
    {
        // Este plugin no necesita servicios adicionales
    }
}
```

Este plugin simplemente se registra en el sistema. Para que haga algo útil, necesitas:
- Agregar un Controller
- Registrar servicios
- Extender la base de datos

---

## Ejemplo: Plugin con Offline Sync

Para soportar modo offline, tu plugin debe:

1. Marcar `OfflineCapable: true` en metadata
2. Implementar sincronización en el frontend

```csharp
public PluginMetadata Metadata => new(
    Id: "my-offline-plugin",
    Name: "My Offline Plugin",
    Version: "1.0.0",
    OfflineCapable: true);  // Importante
```

### Frontend side

El frontend de GymFlow usa Dexie.js (IndexedDB). Tu plugin debe:
- Definir stores en Dexie.js
- Sincronizar datos cuando hay conexión

```typescript
// En tu plugin frontend (src/frontend/src/plugins/my-plugin/)
import Dexie from 'dexie';

export const db = new Dexie('gymflow');
db.version(1).stores({
  my_plugin_data: 'id, memberId, syncStatus'
});
```

### Offline Strategy recomendada

1. **Network-First**: Intenta guardar en API primero
2. **Fallback local**: Si falla, guarda en IndexedDB
3. **Sync queue**: Reintenta sincronización cada 5 minutos

---

## Cómo Agregar un Controller

Los plugins pueden exponer endpoints HTTP:

```csharp
using GymFlow.Plugins.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;

namespace GymFlow.Plugins.MyPlugin;

public class MyPluginController : ControllerBase
{
    [HttpGet("api/my-plugin/hello")]
    public string Hello() => "Hello from plugin!";
}
```

**Nota:** En v1, los controllers deben estar en assemblies separados o registrados manualmente en Program.cs. Futuras versiones permitirán auto-registration.

---

## Cómo Manejar Base de Datos

Los plugins pueden definir entidades adicionales:

### 1. Crear entidad

```csharp
namespace GymFlow.Plugins.MyPlugin;

public class MyPluginEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string MemberId { get; set; } = string.Empty;
    public string Data { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

### 2. Agregar DbSet en DbContext

Esto requiere modificar `GymFlowDbContext`:

```csharp
public DbSet<MyPluginEntity> MyPluginData { get; set; } = null!;
```

### 3. Crear migración

```bash
dotnet ef migrations add AddMyPluginData
dotnet ef database update
```

**Nota:** En v1, las migraciones de plugins deben aplicarse manualmente. future versions will auto-apply.

---

## Testing del Plugin

### Unit tests

```csharp
using Xunit;
using GymFlow.Plugins.MyPlugin;

public class MyPluginTests
{
    [Fact]
    public void Metadata_IsValid()
    {
        var plugin = new MyPlugin();
        
        Assert.NotNull(plugin.Metadata);
        Assert.NotEmpty(plugin.Metadata.Id);
        Assert.NotEmpty(plugin.Metadata.Name);
        Assert.NotEmpty(plugin.Metadata.Version);
    }

    [Fact]
    public void ConfigureServices_DoesNotThrow()
    {
        var plugin = new MyPlugin();
        var services = new ServiceCollection();
        
        plugin.ConfigureServices(services);
        
        Assert.NotEmpty(services);
    }
}
```

### Integración

Para testing de integración:
1. Compila el plugin
2. Copia la DLL al directorio de plugins
3. Inicia GymFlow
4. Verifica en `/api/plugins` que aparece el plugin

---

## Empaquetado del Plugin

### Distribución básica

```bash
dotnet publish -c Release -o ./publish
```

El resultado en `./publish/` contiene:
- `GymFlow.Plugins.MyPlugin.dll`
- Dependencias necesarias

### Distribución como ZIP

```bash
zip -r my-plugin.zip ./publish/*
```

El usuario finale copia el contenido al directorio de plugins y reinicia GymFlow.

---

## Errores Comunes

| Error | Causa | Solución |
|-------|-------|----------|
| `Plugin metadata Id is required` | `Metadata.Id` está vacío | Verifica que `Id` no sea null/empty |
| `Plugin with Id 'x' must have a Name` | `Metadata.Name` faltante | Proporciona un Name válido |
| `Failed to load plugin` | DLL no válida o falta dependencia | Verifica que la DLL sea válida |
| Plugin no aparece en `/api/plugins` | No se descubrió al startup | Reinicia la aplicación |

---

## Limitaciones v1

1. **No hot-reload**: Requiere restart para cargar nuevos plugins
2. **No auto-registration de controllers**: Debe configurarse manualmente
3. **No auto-migrations**: Las migraciones de DB deben aplicarse manualmente
4. **No sandboxing**: Plugins tienen acceso completo al proceso
5. **No version checking**: No hay validación de compatibilidad de versiones
6. **No NuGet**: Referencias via proyecto local

---

## Siguientes Pasos

- [ HU-015 Plugin System](docs/technical/hu15-plugin-system.md) — Documentación técnica completa
- [API Reference](docs/technical/api-reference.md) — Endpoints disponibles
- [Frontend Guide](docs/technical/frontend-guide.md) — Desarrollo frontend