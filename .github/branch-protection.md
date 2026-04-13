# Estrategia de Protección de Ramas

## Ramas Protegidas

GymFlow Lite utiliza tres ramas principales en su estrategia de desarrollo:

- **develop**: Integración de nuevas funcionalidades (entorno de desarrollo).
- **staging**: Validación en un entorno intermedio (pre-producción).
- **main**: Producción estable.

### Reglas para develop y staging
- **Requieren PRs (Pull Requests)** para realizar cambios.
- Se debe aprobar al menos **1 revisión de código**.

### Reglas para main
- **Requiere PRs** (Pull Requests).
- Mínimo de **2 revisiones de código aprobadas**.
- Status checks obligatorios: CI debe ser exitoso.

### Configuración Manual en GitHub
Para configurar manualmente:
1. Ir a "Settings > Branches" del repositorio.
2. En "Branch protection rules", crear una nueva regla para cada rama:
   - develop
   - staging
   - main
3. Configurar cada regla con:
   - Activar "Require a pull request before merging".
   - Activar "Require status checks to pass before merging" y seleccionar:
     - `ci` workflow
     - En staging y main: `security-review` workflow.