#!/bin/bash
# =============================================================================
# GymFlow Lite - Doctor Script
# Pre-flight checks before starting the development environment
# =============================================================================

set -e

# Colors
GREEN='\033[0;32m'
YELLOW='\033[0;33m'
RED='\033[0;31m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

ERRORS=0
WARNINGS=0

log_info() { echo -e "${BLUE}[INFO]${NC} $1"; }
log_ok() { echo -e "${GREEN}[OK]${NC} $1"; }
log_warn() { echo -e "${YELLOW}[WARN]${NC} $1"; ((WARNINGS++)); }
log_error() { echo -e "${RED}[ERROR]${NC} $1"; ((ERRORS++)); }

# =============================================================================
# CHECKS
# =============================================================================

check_docker() {
    log_info "Checking Docker..."
    if command -v docker &> /dev/null; then
        if docker version &> /dev/null; then
            local version=$(docker version --format '{{.Server.Version}}' 2>/dev/null || echo "unknown")
            log_ok "Docker installed (version: $version)"
        else
            log_error "Docker is installed but daemon is not running"
            log_error "Start Docker Desktop or dockerd"
        fi
    else
        log_error "Docker is not installed"
        log_error "Install Docker: https://docs.docker.com/get-docker/"
    fi
}

check_docker_compose() {
    log_info "Checking Docker Compose..."
    if command -v docker compose &> /dev/null; then
        local version=$(docker compose version --short 2>/dev/null || echo "unknown")
        log_ok "Docker Compose v$version installed"
    elif command -v docker-compose &> /dev/null; then
        log_warn "Using legacy docker-compose (v1)"
    else
        log_error "Docker Compose is not installed"
    fi
}

check_ports() {
    log_info "Checking ports..."
    local ports=(3000 5000 5432 6379)
    local occupied=0
    
    for port in "${ports[@]}"; do
        if command -v lsof &> /dev/null; then
            if lsof -i :$port &> /dev/null 2>&1; then
                log_warn "Port $port is in use"
                occupied=1
            fi
        elif command -v ss &> /dev/null; then
            if ss -tlnp 2>/dev/null | grep -q ":$port "; then
                log_warn "Port $port is in use"
                occupied=1
            fi
        fi
    done
    
    if [[ $occupied -eq 0 ]]; then
        log_ok "Required ports available"
    fi
}

check_env_file() {
    log_info "Checking .env file..."
    if [[ -f "docker/.env" ]]; then
        log_ok "docker/.env exists"
        
        # Check for weak passwords
        source docker/.env 2>/dev/null || true
        if [[ "$POSTGRES_PASSWORD" == "changeme" ]]; then
            log_warn "POSTGRES_PASSWORD is still default 'changeme'"
        fi
        if [[ "$JWT_SECRET" == *"32-chars"* ]] || [[ ${#JWT_SECRET} -lt 32 ]]; then
            log_warn "JWT_SECRET appears to be default or too short"
        fi
    else
        log_warn "docker/.env not found"
        log_warn "Run: cp docker/.env.example docker/.env"
    fi
}

check_resources() {
    log_info "Checking system resources..."
    
    # Check disk space (require at least 5GB available)
    local available
    available=$(df -BG . 2>/dev/null | awk 'NR==2 {print $4}' | tr -d 'G')
    if [[ $available -ge 5 ]]; then
        log_ok "Disk space: ${available}GB available"
    else
        log_warn "Low disk space: ${available}GB available (recommend 5GB+)"
    fi
    
    # Check memory (require at least 4GB available)
    if command -v free &> /dev/null; then
        local mem_available
        mem_available=$(free -m 2>/dev/null | awk 'NR==2 {print $7}')
        if [[ $mem_available -ge 4096 ]]; then
            log_ok "Memory: ${mem_available}MB available"
        else
            log_warn "Low memory: ${mem_available}MB available (recommend 4GB+)"
        fi
    fi
}

check_postgres_client() {
    log_info "Checking PostgreSQL client..."
    if command -v psql &> /dev/null; then
        log_ok "psql client installed"
    else
        log_warn "psql client not installed (needed for make shell-postgres)"
    fi
}

check_dotnet() {
    log_info "Checking .NET SDK..."
    if command -v dotnet &> /dev/null; then
        local version=$(dotnet --version 2>/dev/null || echo "unknown")
        log_ok ".NET SDK $version installed"
    else
        log_warn ".NET SDK not installed (needed for migrations)"
    fi
}

# =============================================================================
# MAIN
# =============================================================================

main() {
    echo "=========================================="
    echo -e "${GREEN}GymFlow Lite - Doctor Check${NC}"
    echo "=========================================="
    echo ""
    
    check_docker
    check_docker_compose
    check_ports
    check_env_file
    check_resources
    check_postgres_client
    check_dotnet
    
    echo ""
    echo "=========================================="
    echo -e "Summary: ${GREEN}$ERRORS errors${NC}, ${YELLOW}$WARNINGS warnings${NC}"
    echo "=========================================="
    
    if [[ $ERRORS -gt 0 ]]; then
        echo ""
        log_error "Cannot proceed. Fix the errors above."
        exit 1
    fi
    
    if [[ $WARNINGS -gt 0 ]]; then
        echo ""
        log_warn "Warnings found but can proceed"
        exit 0
    fi
    
    echo ""
    log_ok "All checks passed! Run 'make dev' to start."
    exit 0
}

main "$@"