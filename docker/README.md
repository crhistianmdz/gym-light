# Docker Setup for GymFlow Lite

> Quick reference for running GymFlow Lite locally with Docker Compose.
> For detailed setup, see [docs/technical/local-setup.md](../docs/technical/local-setup.md).

## Prerequisites

| Tool | Version | Notes |
|------|---------|-------|
| Docker | 24+ | [Install](https://docs.docker.com/get-docker/) |
| Docker Compose | v2+ | Included with Docker |
| (Optional) Make | 3.81+ | For `make` commands |

## Quick Start

```bash
# 1. Copy environment file
cp docker/.env.example docker/.env

# 2. Start services
docker compose -f docker/docker-compose.yml up -d

# 3. Verify (should show 4 healthy services)
docker compose -f docker/docker-compose.yml ps

# 4. Access the app
#    Frontend: http://localhost:3000
#    Backend:  http://localhost:5000
```

## Using Make (Recommended)

```bash
# Start all services
make dev

# View logs
make logs

# Stop services
make stop

# Complete reset (removes volumes)
make reset

# Run doctor checks
make doctor
```

## Default Credentials

| Role | Email | Password |
|------|-------|----------|
| Admin | `admin@demo.com` | `admin123` |

## Services

| Service | Port | Health Check |
|--------|------|-------------|
| Frontend (React/Vite) | 3000 | http://localhost:3000 |
| Backend (.NET 8) | 5000 | http://localhost:5000/health |
| PostgreSQL | 5432 | pg_isready |
| Redis | 6379 | redis-cli ping |

## Common Issues

### "Port already in use"

Ports 3000, 5000, 5432, or 6379 may be in use by other applications.

**Solution:**
```bash
# Check what's using the port
lsof -i :3000

# Either stop the other service or modify docker-compose.yml ports
```

### "PostgreSQL connection refused"

The backend starts before PostgreSQL is ready.

**Solution:**
- The docker-compose.yml already has `depends_on` with `condition: service_healthy`
- Wait ~10 seconds for PostgreSQL to initialize on first run

### "Migration failed"

EF Core migrations fail on first run.

**Solution:**
```bash
# Run migrations manually
docker compose exec backend dotnet ef database update

# Or reset the database
make reset
```

### "Invalid JWT Secret"

JWT_SECRET is too short or using default.

**Solution:**
```bash
# Generate a new secret
openssl rand -base64 32 | tr -d '\n'
# Update docker/.env with the new secret
```

### "Docker build fails"

Build errors on backend or frontend.

**Solution:**
```bash
# Clean build artifacts
make clean

# Rebuild
docker compose build --no-cache
```

### "Container exits immediately"

Check logs:
```bash
docker compose logs <service-name>
```

### "Permission denied" on Linux

**Solution:**
```bash
# Add your user to docker group
sudo usermod -aG docker $USER
# Log out and back in
```

## Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `POSTGRES_DB` | Database name | `gymflow_dev` |
| `POSTGRES_USER` | Database user | `gymflow` |
| `POSTGRES_PASSWORD` | Database password | `changeme` |
| `JWT_SECRET` | JWT signing key | (32+ chars) |
| `REDIS_CONNECTION` | Redis host:port | `redis:6379` |

## Management Commands

```bash
# View all logs
docker compose -f docker/docker-compose.yml logs -f

# Access backend shell
docker compose exec backend bash

# Access PostgreSQL
docker compose exec postgres psql -U gymflow -d gymflow_dev

# Run migrations
docker compose exec backend dotnet ef database update

# Restart a service
docker compose restart backend
```

## Stopping

```bash
# Stop services (keeps volumes)
docker compose -f docker/docker-compose.yml down

# Stop and remove volumes (complete reset)
docker compose -f docker/docker-compose.yml down -v
```

## Next Steps

- [Local Setup Guide](../docs/technical/local-setup.md)
- [Architecture Overview](../docs/technical/architecture.md)
- [API Reference](../docs/technical/api-reference.md)