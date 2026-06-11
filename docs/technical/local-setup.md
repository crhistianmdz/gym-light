# Local Setup Guide — GymFlow Lite

> Complete guide for setting up GymFlow Lite development environment locally.
> This document covers Docker Compose setup, troubleshooting, and common workflows.

## Overview

| Component | Technology | Port |
|------------|------------|------|
| Frontend | React + Vite | 3000 |
| Backend | .NET 8 Web API | 5000 |
| Database | PostgreSQL 16 | 5432 |
| Cache | Redis 7 | 6379 |

## Prerequisites

### Linux / macOS

```bash
# Docker
curl -fsSL https://get.docker.com | sh
sudo usermod -aG docker $USER

# Docker Compose (included with Docker Desktop)
docker compose version

# Make (optional, for convenience)
make --version
```

### Windows (WSL2 Recommended)

```powershell
# Install WSL2
wsl --install

# Install Docker Desktop
# https://docs.docker.com/desktop/install/windows-install/

# Open WSL2 terminal and follow Linux instructions
```

### Verify Prerequisites

```bash
docker --version          # Should be 24+
docker compose version   # Should be v2+
make --version          # Optional
```

## Setup Steps

### 1. Clone the Repository

```bash
git clone https://github.com/gymflow/gymflow-lite.git
cd gymflow-lite
```

### 2. Configure Environment

```bash
# Copy example environment file
cp docker/.env.example docker/.env

# Edit with your preferred editor
nano docker/.env  # or code docker/.env
```

**Required changes:**
- `POSTGRES_PASSWORD`: Set a strong password
- `JWT_SECRET`: Generate with `openssl rand -base64 32 | tr -d '\n'`

### 3. Run Doctor Check (Recommended)

```bash
bash scripts/doctor.sh
```

This validates:
- Docker and Docker Compose installed
- Required ports available
- .env file configured
- Sufficient disk space and memory

### 4. Start Services

```bash
# Using Make (recommended)
make dev

# Or directly with Docker Compose
docker compose -f docker/docker-compose.yml up -d
```

Expected output:
```
 ✓ Network gymflow-network created
 ✓ Volume gymflow-postgres-data created
 ✓ Container gymflow-postgres started
 ✓ Container gymflow-redis started
 ✓ Container gymflow-backend started
 ✓ Container gymflow-frontend started
```

### 5. Verify Services

```bash
# Check service health
docker compose ps

# Expected:
# NAME         IMAGE          STATUS
# gymflow-frontend   gymflow-frontend   Up (healthy)
# gymflow-backend   gymflow-backend   Up (healthy)  
# gymflow-postgres   postgres:16-alpine  Up (healthy)
# gymflow-redis     redis:7-alpine    Up (healthy)

# Test endpoints
curl http://localhost:5000/health  # Should return 200
curl http://localhost:3000         # Should return HTML
```

### 6. Access the Application

| Service | URL | Notes |
|---------|-----|-------|
| Frontend | http://localhost:3000 | React app |
| Backend API | http://localhost:5000/api | .NET 8 |
| Swagger UI | http://localhost:5000/swagger | API docs |
| PostgreSQL | localhost:5432 | DB client |

**Login credentials:**
- Email: `admin@demo.com`
- Password: `admin123`

## Daily Development Workflows

### View Logs

```bash
# All services
make logs

# Specific service
make logs-backend
make logs-frontend
make logs-postgres
make logs-redis
```

### Run Migrations

```bash
# Apply pending migrations
make migrate

# Create new migration
make migrate-create name=AddNewFeature
```

### Access Containers

```bash
# Backend shell
make shell-backend

# PostgreSQL CLI
make shell-postgres
```

### Reset Environment

```bash
# Stop and remove all data
make reset

# Start fresh
make dev
```

### Run Tests

```bash
# Backend tests
docker compose exec backend dotnet test

# Specific test project
docker compose exec backend dotnet test --filter "FullyQualifiedName~MemberTests"
```

## Troubleshooting

### Ports Already in Use

```bash
# Find what's using the port
lsof -i :3000
lsof -i :5000
lsof -i :5432
lsof -i :6379

# Stop the conflicting service
sudo kill -9 <PID>
```

### Container Won't Start

```bash
# Check logs
docker compose logs <service>

# Check health status
docker inspect gymflow-backend --format='{{.State.Health.Status}}'
```

### Database Connection Errors

```bash
# Verify PostgreSQL is running
docker compose ps postgres

# Check connection
docker compose exec postgres psql -U gymflow -d gymflow_dev -c "SELECT 1"

# Reset database
make reset
```

### JWT Errors

```bash
# Generate new secret
openssl rand -base64 32 | tr -d '\n'

# Update docker/.env
```

### Permission Denied (Linux)

```bash
# Fix docker group
sudo chown -R $USER:$USER .
sudo chmod -R 755 .
```

### Out of Disk Space

```bash
# Clean Docker
docker system prune -af

# Remove unused volumes
docker volume prune
```

### Slow First Build

```bash
# First build takes 3-5 minutes (normal)
# .NET SDK image is ~600MB
# Subsequent builds are incremental
```

## Project Structure

```
gymflow-lite/
├── docker/
│   ├── docker-compose.yml    # Service definitions
│   ├── .env.example        # Environment template
│   ├── backend/
│   │   └── Dockerfile     # .NET 8 container
│   └── frontend/
│       └── Dockerfile     # React + Vite container
├── src/
│   ├── backend/             # .NET 8 solution
│   │   ├── Domain/         # Entities, interfaces
│   │   ├── Application/    # Use cases
│   │   ├── Infrastructure/# DB, Redis, services
│   │   └── WebAPI/        # HTTP endpoints
│   └── frontend/           # React + TypeScript
├── scripts/
│   └── doctor.sh         # Pre-flight checks
├── Makefile              # Convenience commands
└── docs/
    └── technical/
        └── local-setup.md # This file
```

## Seed Data

On first run, the following data is seeded:

| Entity | Count | Notes |
|--------|-------|-------|
| Admin User | 1 | admin@demo.com / admin123 |
| Members | 10 | Demo users with varying membership dates |
| Products | 10 | POS inventory items |

## Next Steps

- [Architecture Overview](architecture.md)
- [API Reference](api-reference.md)
- [Frontend Guide](frontend-guide.md)
- [Contributing Guide](../CONTRIBUTING.md)

## See Also

- [Docker README](../docker/README.md)
- [ADR-001: Technology Stack](../architecture/adr/001-stack-tecnologico.md)
- [ADR-007: Self-Hosted Model](../architecture/adr/007-modelo-self-hosted.md)