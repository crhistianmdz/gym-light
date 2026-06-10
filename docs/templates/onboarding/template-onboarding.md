# ONBOARDING-XXX: [Nombre del Onboarding]

**Audience**: Nuevos devs en el proyecto
**Time to read**: X minutos
**Last Updated**: YYYY-MM-DD

---

## 🎯 Bienvenida

1 párrafo. ¿Qué es este proyecto? ¿Por qué es interesante?

---

## 📚 Stack Tecnológico

### Backend
- **Lenguaje**: C# (.NET 8)
- **Framework**: ASP.NET Core Web API
- **ORM**: Entity Framework Core
- **DB**: PostgreSQL 16 (Docker)
- **Cache**: Redis
- **Auth**: JWT + HttpOnly Cookies

### Frontend
- **Lenguaje**: TypeScript
- **Framework**: React 18 + Vite
- **UI**: Material-UI
- **State**: [Redux/Zustand/Señales]
- **Offline**: Dexie.js (IndexedDB)
- **PWA**: Service Workers + Web Manifest

### Infraestructura
- **Containers**: Docker + docker-compose
- **CI/CD**: GitHub Actions
- **Hosting**: [a definir]

---

## 🚀 Setup Local

### Prerrequisitos

- [ ] Docker + Docker Compose
- [ ] Node.js 20+
- [ ] .NET SDK 8.0
- [ ] Git

### Pasos

```bash
# 1. Clonar repo
git clone <repo-url>
cd gym-light

# 2. Levantar DB + Redis
docker compose -f docker/docker-compose.yml up -d postgres redis

# 3. Backend
cd src/backend/WebAPI
dotnet restore
dotnet run

# 4. Frontend (en otra terminal)
cd src/frontend
npm install
npm run dev
```

### URLs locales

- Frontend: http://localhost:5173
- Backend API: http://localhost:5000
- Swagger: http://localhost:5000/swagger
- pgAdmin: http://localhost:5050

---

## 📂 Estructura del Proyecto

```
src/
├── backend/          ← Clean Architecture
│   ├── Domain/       ← Entidades + reglas de negocio puras
│   ├── Application/  ← Use cases
│   ├── Infrastructure/ ← EF, DB, externos
│   └── WebAPI/       ← Controllers, DTOs, Middleware
└── frontend/
    ├── src/
    │   ├── app/      ← Entry point, routing
    │   ├── features/ ← Por feature (auth, members, sales)
    │   ├── shared/   ← Componentes reutilizables
    │   └── lib/      ← API client, utils
    └── public/       ← Assets, manifest, service worker

docs/
├── PRD_*.md          ← Product Requirements
├── RFC_*.md          ← Decisiones técnicas aprobadas
├── technical/        ← Docs técnicas
├── tasks/            ← HUs y user stories
└── templates/        ← Templates de documentos
```

---

## 🏛️ Arquitectura

### Backend (Clean Architecture)

- **Domain**: NO depende de nada. Entidades + reglas de negocio.
- **Application**: Depende solo de Domain. Use cases, DTOs, interfaces.
- **Infrastructure**: Depende de Application. EF Core, DB, servicios externos.
- **WebAPI**: Depende de Application + Infrastructure. Controllers, middleware, DI.

### Frontend (Feature-based)

- **app/**: Routing global, providers, layout principal
- **features/**: Una carpeta por feature (auth, members, sales, etc.)
- **shared/**: Componentes UI, hooks, utils reutilizables
- **lib/**: API client, config, helpers

---

## 📋 Reglas de Negocio Clave

> Estas vienen del PRD. **No las rompas**.

- **Acceso**: Denegar si `status === 'Frozen' || status === 'Expired'`
- **Foto obligatoria**: El botón de guardado se deshabilita si no hay foto
- **ClientGuid**: Toda escritura local genera UUID v4 (idempotencia)
- **Stock**: Bloquear venta si stock local es 0
- **Server wins**: En conflicto local vs. nube, el servidor gana

Ver `AGENTS.md` sección 4 para la lista completa.

---

## 🔄 Flujo de Trabajo

### Crear una HU nueva

1. Copiar `docs/templates/hu/template-hu-simple.md` (o `template-hu-sdd.md`)
2. Nombrar `docs/tasks/HU-XXX-HU-YYY/HU-NNN-nombre.md`
3. Llenar el template
4. Implementar siguiendo TDD
5. Code review
6. Merge

### Git Workflow

- Rama `main`: siempre deployable
- Rama `feature/HU-NNN-descripcion`: para features
- Rama `fix/bug-descripcion`: para bugfixes
- Commits: conventional commits (`feat:`, `fix:`, `docs:`, `refactor:`)

---

## 🔗 Links Útiles

- [PRD del proyecto](docs/PRD_GymFlow_Lite.md)
- [RFCs aprobadas](docs/)
- [Documentación técnica](docs/technical/)
- [Plantillas](docs/templates/)
- [AGENTS.md (reglas para AI)](AGENTS.md)
- Swagger (en dev): http://localhost:5000/swagger

---

## ❓ FAQ

**P: ¿Cómo debuggeo offline sync?**
R: Inspeccionar IndexedDB con DevTools → Application → IndexedDB → gymflow-db

**P: ¿Cómo agrego un endpoint nuevo?**
R: 1) Endpoint en WebAPI, 2) Use case en Application, 3) Test, 4) Doc con template `api-endpoint`

**P: ¿Dónde van las migraciones de DB?**
R: `src/backend/Infrastructure/Persistence/Migrations/`. Ver AGENTS.md sección 6.
