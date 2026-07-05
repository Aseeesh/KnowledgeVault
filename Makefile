# KnowledgeVault — local development commands
# Usage: make <target>

.DEFAULT_GOAL := help
.PHONY: help setup up down restart logs status test test-api test-ai test-security \
        bench eval pull-models shell-api shell-ai clean nuke

# ─── Colors ───────────────────────────────────────────────────────────────────
CYAN  := \033[0;36m
GREEN := \033[0;32m
YELLOW:= \033[0;33m
RESET := \033[0m

help: ## Show this help
	@echo ""
	@echo "  $(CYAN)KnowledgeVault$(RESET) — local dev commands"
	@echo ""
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) | \
		awk 'BEGIN {FS = ":.*?## "}; {printf "  $(GREEN)%-18s$(RESET) %s\n", $$1, $$2}'
	@echo ""

# ─── Setup ────────────────────────────────────────────────────────────────────

setup: ## First-time setup: copy .env and build images
	@echo "$(CYAN)Setting up KnowledgeVault...$(RESET)"
	@if [ ! -f .env ]; then \
		cp .env.example .env; \
		echo "$(GREEN)Created .env from .env.example$(RESET)"; \
	else \
		echo "$(YELLOW).env already exists, skipping$(RESET)"; \
	fi
	@docker compose build
	@echo "$(GREEN)Setup complete. Run 'make up' to start.$(RESET)"

# ─── Lifecycle ────────────────────────────────────────────────────────────────

up: ## Start all services (pulls Ollama models automatically)
	@echo "$(CYAN)Starting KnowledgeVault...$(RESET)"
	@docker compose up -d
	@echo ""
	@echo "$(CYAN)Waiting for Ollama model pull to complete...$(RESET)"
	@docker compose logs -f ollama-pull 2>/dev/null || true
	@echo ""
	@echo "$(GREEN)All services started:$(RESET)"
	@echo "  Web Client  → http://localhost:5173"
	@echo "  API         → http://localhost:5001/swagger"
	@echo "  AI Engine   → http://localhost:8000/docs"
	@echo "  Qdrant      → http://localhost:6333/dashboard"
	@echo "  RabbitMQ    → http://localhost:15672  ($(shell grep RABBITMQ_USER .env 2>/dev/null | cut -d= -f2 || echo kv_user) / $(shell grep RABBITMQ_PASSWORD .env 2>/dev/null | cut -d= -f2 || echo KvRabbit2024!))"

up-build: ## Rebuild images and start
	@docker compose up -d --build
	@docker compose logs -f ollama-pull 2>/dev/null || true

down: ## Stop all services
	@docker compose down
	@echo "$(GREEN)All services stopped.$(RESET)"

restart: ## Restart all services
	@docker compose restart

restart-api: ## Restart only the API
	@docker compose restart api

restart-ai: ## Restart only the AI engine
	@docker compose restart ai-engine

# ─── Monitoring ───────────────────────────────────────────────────────────────

status: ## Show service health status
	@echo "$(CYAN)Service status:$(RESET)"
	@docker compose ps --format "table {{.Name}}\t{{.Status}}\t{{.Ports}}"

logs: ## Tail logs from all services
	@docker compose logs -f

logs-api: ## Tail API logs
	@docker compose logs -f api

logs-ai: ## Tail AI engine logs
	@docker compose logs -f ai-engine

logs-ollama: ## Tail Ollama logs
	@docker compose logs -f ollama

# ─── Testing ──────────────────────────────────────────────────────────────────

test: test-api test-ai ## Run all tests (C# + Python)

test-api: ## Run C# unit + integration tests (xUnit)
	@echo "$(CYAN)Running C# tests...$(RESET)"
	@cd api && dotnet test --logger "console;verbosity=normal" 2>&1 | tail -30

test-ai: ## Run Python AI engine tests (pytest)
	@echo "$(CYAN)Running Python tests...$(RESET)"
	@docker exec kv-ai-engine python -m pytest tests/ -v --tb=short 2>&1 | tail -40

test-security: ## Run security test suite (16 tests)
	@echo "$(CYAN)Running security tests...$(RESET)"
	@bash tests/security/security-test.sh

bench: ## Run performance benchmarks
	@echo "$(CYAN)Running benchmarks...$(RESET)"
	@bash tests/benchmarks/run-benchmarks.sh

eval: ## Run AI quality evaluation (golden dataset)
	@echo "$(CYAN)Running evaluation against golden dataset...$(RESET)"
	@curl -s -X POST http://localhost:8000/eval/run | python3 -m json.tool

# ─── Models ───────────────────────────────────────────────────────────────────

pull-models: ## Manually pull Ollama models
	@echo "$(CYAN)Pulling Ollama models...$(RESET)"
	@docker exec kv-ollama ollama pull llama3.2:1b
	@docker exec kv-ollama ollama pull nomic-embed-text
	@echo "$(GREEN)Models ready.$(RESET)"

list-models: ## List available Ollama models
	@docker exec kv-ollama ollama list

# ─── Shell access ─────────────────────────────────────────────────────────────

shell-api: ## Open shell in API container
	@docker exec -it kv-api bash

shell-ai: ## Open shell in AI engine container
	@docker exec -it kv-ai-engine bash

shell-db: ## Open psql in PostgreSQL container
	@docker exec -it kv-postgres psql -U kv_user -d knowledgevault

shell-redis: ## Open redis-cli
	@docker exec -it kv-redis redis-cli -a $$(grep REDIS_PASSWORD .env | cut -d= -f2)

# ─── Smoke tests ──────────────────────────────────────────────────────────────

smoke: ## Run quick smoke test against live services
	@echo "$(CYAN)Smoke testing services...$(RESET)"
	@echo -n "  API health:        "; curl -sf http://localhost:5001/health | python3 -m json.tool 2>/dev/null | grep status || echo "FAIL"
	@echo -n "  AI engine health:  "; curl -sf http://localhost:8000/health | python3 -m json.tool 2>/dev/null | grep status || echo "FAIL"
	@echo -n "  Qdrant health:     "; curl -sf http://localhost:6333/healthz && echo "OK" || echo "FAIL"
	@echo -n "  Ollama models:     "; curl -sf http://localhost:11434/api/tags | python3 -c "import sys,json; m=json.load(sys.stdin)['models']; print(', '.join(x['name'] for x in m) if m else 'no models')"
	@echo ""
	@echo "$(CYAN)Testing chat endpoint...$(RESET)"
	@curl -s -X POST http://localhost:5001/api/v1/chat/completions \
		-H "X-Tenant-Id: a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11" \
		-H "Content-Type: application/json" \
		-d '{"message":"Hello, what can you help me with?"}' | python3 -m json.tool

# ─── Cleanup ──────────────────────────────────────────────────────────────────

clean: ## Remove containers and images (keeps volumes)
	@docker compose down --rmi local
	@echo "$(GREEN)Containers and images removed. Data volumes preserved.$(RESET)"

nuke: ## Remove EVERYTHING including volumes (fresh start)
	@echo "$(YELLOW)This will delete all data (postgres, qdrant, redis, ollama models).$(RESET)"
	@read -p "Continue? [y/N] " confirm && [ "$$confirm" = "y" ] || exit 1
	@docker compose down -v --rmi local
	@echo "$(GREEN)Clean slate. Run 'make setup && make up' to start fresh.$(RESET)"
