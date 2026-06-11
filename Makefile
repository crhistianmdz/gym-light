# GymFlow Lite - Makefile for local development
# Usage: make <target>

# =============================================================================
# VARIABLES
# =============================================================================
COMPOSE_FILE := docker/docker-compose.yml
COMPOSE := docker compose -f $(COMPOSE_FILE)
ENV_FILE := docker/.env

# Colors
GREEN := \033[0;32m
YELLOW := \033[0;33m
RED := \033[0;31m
NC := \033[0m # No Color

# =============================================================================
# TARGETS
# =============================================================================

.PHONY: help dev start stop reset logs logs-backend logs-frontend logs-postgres logs-redis
.PHONY: seed clean doctor status ps

help:
	@echo "GymFlow Lite - Available targets:"
	@echo ""
	@echo "  make dev          Start all services (alias for make start)"
	@echo "  make start       Start all services in detached mode"
	@echo "  make stop        Stop all services"
	@echo "  make reset      Stop and remove all volumes (complete reset)"
	@echo "  make logs       Tail all service logs"
	@echo "  make logs-<svc> Tail specific service logs (backend|frontend|postgres|redis)"
	@echo "  make seed       Run database seed script"
	@echo "  make doctor     Run pre-flight checks"
	@echo "  make status    Show service status"
	@echo "  make ps        Show running containers"
	@echo "  make clean     Remove containers and build artifacts"
	@echo ""
	@echo "  make migrate   Run EF Core migrations"
	@echo "  make shell-backend  Open bash in backend container"
	@echo "  make shell-postgres Open psql in postgres container"

dev start:
	@$(COMPOSE) up -d
	@echo "$(GREEN)✓ GymFlow Lite started$(NC)"
	@echo "  Backend:  http://localhost:5000"
	@echo "  Frontend: http://localhost:3000"
	@echo "  API:      http://localhost:5000/api"

stop:
	@$(COMPOSE) stop

reset: stop
	@$(COMPOSE) down -v
	@echo "$(YELLOW)✓ All volumes removed. Run 'make dev' to start fresh.$(NC)"

logs:
	@$(COMPOSE) logs -f

logs-backend:
	@$(COMPOSE) logs -f backend

logs-frontend:
	@$(COMPOSE) logs -f frontend

logs-postgres:
	@$(COMPOSE) logs -f postgres

logs-redis:
	@$(COMPOSE) logs -f redis

seed:
	@echo "Seeding database..."
	@$(COMPOSE) exec -e ASPNETCORE_ENVIRONMENT=Development backend dotnet ef database seed || echo "$(YELLOW)⚠ Seed command not available. Run migrations first.$(NC)"

doctor:
	@echo "Running pre-flight checks..."
	@bash scripts/doctor.sh

status:
	@echo "=== Service Status ==="
	@$(COMPOSE) ps
	@echo ""
	@echo "=== Resource Usage ==="
	@$(COMPOSE) stats --no-stream 2>/dev/null || true

ps:
	@$(COMPOSE) ps

clean:
	@$(COMPOSE) down --remove-orphans
	@rm -rf docker/backend/bin docker/backend/obj
	@rm -rf docker/frontend/dist
	@echo "$(GREEN)✓ Cleaned build artifacts$(NC)"

# =============================================================================
# DEVELOPMENT
# =============================================================================

migrate:
	@echo "Running EF Core migrations..."
	@$(COMPOSE) exec backend dotnet ef database update

migrate-create name=$(name):
	@$(COMPOSE) exec backend dotnet ef migrations add $(name)

shell-backend:
	@$(COMPOSE) exec backend bash

shell-postgres:
	@$(COMPOSE) exec postgres psql -U $(POSTGRES_USER) -d $(POSTGRES_DB)