# Spec arquitectura de despliegue cicd

## 1. Contexto y objetivo

Implementar ambientes de dev (develeop), stg (stagin) y prod (produccion), flujo de integracion y entrega continua **Github Actions** para garantizar que ningun codigo incompleto o roto llegue a produccion

## 2. Infraestructura

- **Orquestador:** GitHub Actions [.yml] [3].
- **Estrategia de Ramas:** Toda funcionalidad entra vía **Pull Request (PR)**; prohibido push directo a `main`.
- **Seguridad:** Inclusión obligatoria de la acción `claude-code-security-review` en cada PR.

## 3  Flujo de Integración Continua (CI)

El agente debe configurar un workflow que ejecute secuencialmente:

1. **Restore:** Restauración de paquetes NuGet (.NET 8) y npm (React).
2. **Lint:** Validación de estilos de código para C# y ESLint para React.
3. **Build:** Compilación de la solución .NET y el build de Vite/React.
4. **Test:** Ejecución de tests unitarios con **xUnit** (Backend) y **Vitest** (Frontend). El merge se bloquea si los tests fallan [5, 6].

## 4. Estrategia de CD y Versiones

- Implementar **Release Please** de Google para automatizar el versionado y la creación de etiquetas (tags) en Git [7].
- Generación automática de *Release Notes* utilizando IA tras cada merge exitoso [7].