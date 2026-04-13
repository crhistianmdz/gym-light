# Estructura de Carpetas — GymFlow Lite

## Raíz
```
/
├── src/                     ← Código fuente del proyecto
│   ├── backend/             ← Solución .NET 8: Clean Architecture para el backend
│   └── frontend/            ← PWA basada en React + Vite
├── docs/                    ← Documentación de producto y técnica
├── docker/                  ← Archivos Docker para entornos dev y prod
├── AGENTS.md                ← Guía operativa para agentes IA
```

## Backend (`src/backend/`)
```
/src/backend/
├── WebAPI/                 ← Proyecto principal del backend (Controllers y configuración)
│   ├── Program.cs          ← Punto de entrada de la aplicación WebAPI
│   ├── Controllers/        ← Controladores para manejar las rutas HTTP
│   │   ├── Admin/          ← Controladores específicos del rol admin
│   │   │   └── MetricsController.cs
│   ├── Extensions/         ← Métodos de extensión para ClaimsPrincipal y Auth
│   │   ├── ClaimsPrincipalExtensions.cs
│   │   └── AuthExtensions.cs
│   └── Filters/            ← Filtros personalizados
├── Application/            ← Contiene lógica de negocio, DTOs y validaciones
│   ├── UseCases/           ← Casos de uso específicos del negocio
│   ├── DTOs/               ← Objeto de transferencia de datos
│   └── Validators/         ← Validaciones de entrada
├── Domain/                 ← Entidades, enumeraciones e interfaces del dominio
│   ├── Entities/           ← Modelos principales del dominio
│   ├── Enums/              ← Enumeraciones para clasificaciones
│   ├── Interfaces/         ← Interfaces para repositorios
│   ├── Exceptions/         ← Excepciones definidas en el dominio
│   │   └── DomainException.cs
│   └── Models/             ← Modelos complejos
├── Infrastructure/         ← Implementaciones de persistencia y servicios externos
│   ├── Migrations/         ← Migraciones EF Core para la base de datos
│   ├── Persistence/        ← Configuración de EF Core, repositorios y seeding
│   ├── Services/           ← Servicios generales como el almacenamiento de fotos
│   │   └── LocalPhotoStorageService.cs
│   └── Security/           ← Configuración de servicios de seguridad como JWT
└── Tests/                  ← Pruebas unitarias y de integración
    ├── Domain/             ← Pruebas enfocadas a la lógica de dominio
    └── UseCases/           ← Pruebas de los casos de uso
```

## Frontend (`src/frontend/`)
```
/src/frontend/
├── src/                    ← Código fuente de la app React
│   ├── db/                 ← Base local IndexedDB usando Dexie.js
│   ├── services/           ← Servicios para lógica de red y sincronización
│   ├── components/         ← Componentes reutilizables
│   ├── pages/              ← Vistas principales de la app
│   ├── hooks/              ← Custom Hooks para lógica reutilizable
│   ├── vite-env.d.ts       ← Declaraciones de tipo para Vite
│   └── router.tsx          ← Definición de rutas de la aplicación
├── vite.config.ts          ← Configuración del bundler
├── tsconfig.node.json      ← Configuración de TypeScript para NodeJS
└── package.json            ← Dependencias de frontend
```

## Docs (`docs/`)
```
/docs/
├── PRD_GymFlow_Lite.md             ← Requisitos y definición de producto (PRD)
├── RFC_001_Architecture_Offline_Sync.md ← Decisiones técnicas aprobadas
├── User_Stories_GymFlow.md         ← Historias de usuario para el proyecto
└── technical/                      ← Documentación técnica
    ├── api-reference.md            ← Referencia detallada de la API
    ├── architecture.md             ← Principios generales de arquitectura
    ├── frontend-guide.md           ← Guía de desarrollo frontend
    ├── hu01-checkin.md             ← Documentación técnica para el HU 01
    ├── hu02-member-registration.md ← HU sobre registro de miembros
    ├── hu03-sales.md               ← HU relacionada a ventas
    ├── hu04-sync.md                ← HU sobre sincronización offline
    ├── hu05-auth.md                ← Autenticación y autorización
    ├── hu07-freeze.md              ← HU sobre congelamiento de membresías
    ├── hu08-cancellation.md        ← Proceso de cancelaciones
    ├── hu09-anthropometry.md       ← HU para antropometría
    ├── hu10-progress-chart.md      ← HU sobre gráficos de progreso
    └── hu11-routines.md            ← HU sobre rutinas personalizadas
```

## Docker (`docker/`)
```
/docker/
├── docker-compose.yml  ← Configuración principal de Docker Compose
├── backend/            ← Configuración Docker para el backend
│   └── Dockerfile
├── frontend/           ← Configuración Docker para el frontend
│   └── Dockerfile
├── .env                ← Variables de entorno para configuración
├── .env.example        ← Ejemplo de archivo de variables de entorno
└── README.md           ← Información sobre las configuraciones Docker
```