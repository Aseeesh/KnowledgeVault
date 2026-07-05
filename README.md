# KnowledgeVault

A production-grade **Retrieval-Augmented Generation (RAG)** platform that lets users upload documents and ask questions — receiving AI-generated answers grounded in their knowledge base with verified citations.

Built as a portfolio project demonstrating microservices architecture, hybrid search algorithms, local LLM inference, and end-to-end deployment on Azure.

<br/>

## What It Does

Upload documents → the system chunks, embeds, and indexes them → ask questions in natural language → get answers with source citations and confidence scores.

Every answer is **grounded** in your documents. Citations are **verified** against source text. Hallucinations are **detected** and flagged.

<br/>

## Key Features

| Feature | Implementation |
|---------|---------------|
| **Hybrid Search** | Dense vectors (Qdrant) + BM25 sparse retrieval, merged via Reciprocal Rank Fusion |
| **Citation Verification** | Embedding-based claim matching with confidence scoring and hallucination detection |
| **Multi-Tenant** | Tenant isolation on every query — API key or header-based authentication |
| **Async Ingestion** | Document upload → RabbitMQ queue → chunking → embedding → vector indexing |
| **Streaming Chat** | Server-Sent Events for token-by-token response delivery |
| **Local LLM** | Runs entirely offline using Ollama (llama3.2, nomic-embed-text) |
| **Prompt Versioning** | YAML-based templates with hot-reload and automated evaluation |
| **Admin Dashboard** | Live service health, document management, quality metrics |

<br/>

## Architecture

```
┌─────────────┐     ┌──────────┐     ┌────────────────────┐
│  React 19   │────▶│  Nginx   │────▶│   .NET 9 API       │──▶ PostgreSQL
│  TypeScript │     │  Gateway │     │   MediatR CQRS     │──▶ Redis
│  Tailwind   │     │          │     │   Multi-tenant     │──▶ RabbitMQ
└─────────────┘     └──────────┘     └─────────┬──────────┘
                                               │
                                     ┌─────────▼──────────┐
                                     │  Python FastAPI     │──▶ Qdrant
                                     │  RAG Pipeline       │──▶ Ollama
                                     │  Prompt Management  │──▶ Redis
                                     └────────────────────┘
```

**9 services** running in Docker Compose. 3 custom applications + 6 infrastructure services.

<br/>

## Tech Stack

| Layer | Technology |
|-------|-----------|
| **API** | C# .NET 9, MediatR, FluentValidation, EF Core, Serilog |
| **AI Engine** | Python 3.12, FastAPI, Qdrant client, rank-bm25, tenacity |
| **Frontend** | React 19, TypeScript, Vite 6, Tailwind CSS, Zustand, React Query |
| **LLM** | Ollama (llama3.2:1b, nomic-embed-text) |
| **Vector DB** | Qdrant (cosine similarity, payload-filtered tenant isolation) |
| **Database** | PostgreSQL 16 (EF Core + tsvector full-text search) |
| **Cache** | Redis 7 (embedding cache 24hr TTL, search cache 5min TTL) |
| **Queue** | RabbitMQ (document ingestion with dead-letter handling) |
| **Gateway** | Nginx (reverse proxy, security headers, routing) |
| **Infrastructure** | Docker Compose, Terraform, GitHub Actions CI/CD |
| **Cloud** | Azure (App Service, PostgreSQL Flexible, Redis Cache, Storage, ACR) |

<br/>

## Quick Start

### Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) — 8 GB RAM recommended (6 GB minimum)
- `make` (pre-installed on macOS/Linux; Windows: use Git Bash or WSL)
- `curl`, `python3` (used by smoke tests and Makefile helpers)
- `dotnet` SDK 9 (only needed to run C# tests locally outside Docker)

> **Ollama runs inside Docker** — no local Ollama installation needed. Models are pulled automatically on first start.

---

### Option A — Using Make (recommended)

```bash
# 1. Clone and enter the project
git clone <repo-url>
cd KnowledgeVault

# 2. First-time setup: creates .env + builds images
make setup

# 3. Start all 9 services (Ollama models pulled automatically)
make up
```

That's it. `make up` blocks until the `ollama-pull` service finishes downloading `llama3.2:1b` and `nomic-embed-text`, then prints the service URLs.

---

### Option B — Using Docker Compose directly

```bash
git clone <repo-url>
cd KnowledgeVault
cp .env.example .env
docker compose up --build -d

# Wait for model pull to finish (takes 2–10 min on first run)
docker compose logs -f ollama-pull
```

---

### Service URLs

| Service | URL | Credentials |
|---------|-----|-------------|
| **Web Client** | http://localhost:5173 | — |
| **API Swagger** | http://localhost:5001/swagger | — |
| **AI Engine Docs** | http://localhost:8000/docs | — |
| **Qdrant Dashboard** | http://localhost:6333/dashboard | — |
| **RabbitMQ UI** | http://localhost:15672 | `kv_user` / `KvRabbit2024!` |
| **Ollama** | http://localhost:11434 | — |

---

### Verify everything works

```bash
make smoke
```

This checks health endpoints on all services, lists loaded Ollama models, and sends a test chat message end-to-end.

<br/>

## Project Structure

```
KnowledgeVault/
├── api/                          # C# .NET 9 REST API
│   ├── src/
│   │   ├── KnowledgeVault.Core/         # Entities, interfaces, DTOs
│   │   ├── KnowledgeVault.Application/  # MediatR commands/queries, services
│   │   ├── KnowledgeVault.Infrastructure/ # EF Core, Redis, Qdrant, RabbitMQ
│   │   └── KnowledgeVault.API/          # Controllers, middleware, background services
│   └── tests/                    # xUnit + FluentAssertions + Moq (35 tests)
│
├── ai-engine/                    # Python FastAPI AI Service
│   ├── app/
│   │   ├── clients/ollama.py            # Connection pooling, retry, streaming
│   │   ├── services/                    # Embedding, search, RAG pipeline, token counter
│   │   ├── prompts/                     # YAML templates, version manager
│   │   ├── evaluation/                  # Golden dataset, automated eval
│   │   └── routers/                     # REST endpoints
│   └── tests/                    # pytest (46 tests)
│
├── web-client/                   # React 19 Frontend
│   └── src/
│       ├── components/                  # Chat, citations, documents, dashboard
│       ├── hooks/                       # useChat (SSE streaming), useDocuments
│       ├── store/                       # Zustand state management
│       └── pages/                       # Chat, Search, Documents, Dashboard
│
├── infrastructure/
│   ├── docker/                   # Dockerfiles (API, AI, Web)
│   ├── nginx/                    # Reverse proxy config
│   ├── terraform/                # Azure IaC (6 modules)
│   └── scripts/                  # DB init, deployment
│
├── tests/
│   ├── benchmarks/               # Performance benchmark suite
│   ├── performance/              # k6 load test scripts
│   └── security/                 # Security test suite
│
├── docs/
│   ├── architecture.md           # C4 diagrams, data flows, security
│   ├── adr/                      # 5 Architecture Decision Records
│   ├── production-readiness.md   # Checklist
│   └── azure-cost-estimation.md  # Cost analysis
│
├── docker-compose.yml            # 9-service local development
└── .github/workflows/            # CI, Deploy, Benchmark pipelines
```

<br/>

## API Endpoints

### REST API (port 5001)

```bash
# Documents
GET    /api/v1/documents                    # List (paginated)
POST   /api/v1/documents                    # Create
POST   /api/v1/documents/upload             # Upload file + auto-ingest
GET    /api/v1/documents/{id}               # Get by ID
DELETE /api/v1/documents/{id}               # Delete

# Search
POST   /api/v1/search                       # Hybrid search (dense + BM25 + RRF)

# Chat
POST   /api/v1/chat/completions             # RAG chat (JSON response)
POST   /api/v1/chat/completions/stream      # RAG chat (SSE streaming)
GET    /api/v1/chat/sessions/{id}           # Session history
POST   /api/v1/chat/messages/{id}/feedback  # Submit feedback

# Ingestion
GET    /api/v1/ingestion/jobs/{id}          # Job status
```

All endpoints require `X-Tenant-Id` header or `Authorization: Bearer <api-key>`.

### AI Engine (port 8000)

```bash
POST   /embeddings/          # Generate embeddings
POST   /search/hybrid        # Hybrid search
POST   /generate/            # Generate with context
POST   /generate/rag         # Full RAG pipeline
POST   /generate/stream      # SSE streaming
GET    /models/              # List Ollama models
POST   /eval/run             # Run evaluation suite
GET    /prompts/version      # Prompt version info
GET    /metrics              # Prometheus metrics
```

### Example: Chat Completion

```bash
curl -X POST http://localhost:5001/api/v1/chat/completions \
  -H "X-Tenant-Id: a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11" \
  -H "Content-Type: application/json" \
  -d '{"message": "What is RAG?"}'
```

Response:
```json
{
  "sessionId": "...",
  "messageId": "...",
  "answer": "RAG combines retrieval with generation [Source 1]...",
  "citations": [
    {
      "chunkId": "...",
      "documentTitle": "RAG Architecture Guide",
      "excerpt": "Retrieval-Augmented Generation combines...",
      "confidence": 0.95,
      "verified": true
    }
  ],
  "confidenceScore": 0.71,
  "hasPotentialHallucinations": false
}
```

<br/>

## Performance

Benchmarked on local Docker (MacOS, 8GB RAM, CPU-only Ollama):

| Endpoint | P50 | P95 |
|----------|-----|-----|
| Document CRUD | 32ms | 49ms |
| Embedding Generation | 40ms | 266ms |
| Hybrid Search | 35ms | 68ms |
| RAG Pipeline | 2,487ms | 3,829ms |
| Chat Completion | 2,571ms | 2,806ms |

- **0% error rate** across all benchmarks
- **56% Redis cache hit rate** for embeddings
- **85.5% answer similarity** on golden dataset evaluation
- **102 tests** passing across C#, Python, and security suites

<br/>

## Testing

### Run all tests

```bash
make test           # C# + Python (runs locally / inside containers)
make test-security  # Security suite (requires services to be running)
make eval           # AI quality evaluation against golden dataset
make bench          # Performance benchmarks
```

### Individual suites

```bash
# C# unit + integration tests (35 tests)
cd api && dotnet test --logger "console;verbosity=normal"

# Python AI engine tests (46 tests, inside container)
docker exec kv-ai-engine python -m pytest tests/ -v --tb=short

# Security tests — runs against live services (make sure 'make up' first)
bash tests/security/security-test.sh

# Performance benchmarks
bash tests/benchmarks/run-benchmarks.sh

# AI quality evaluation (golden dataset — 5 cases)
curl -s -X POST http://localhost:8000/eval/run | python3 -m json.tool
```

### Manual end-to-end test

```bash
# 1. Upload a document
curl -X POST http://localhost:5001/api/v1/documents/upload \
  -H "X-Tenant-Id: a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11" \
  -F "file=@your-document.txt" \
  -F "title=My Document"

# 2. Wait for indexing (check status)
curl http://localhost:5001/api/v1/documents \
  -H "X-Tenant-Id: a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11"

# 3. Ask a question
curl -X POST http://localhost:5001/api/v1/chat/completions \
  -H "X-Tenant-Id: a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11" \
  -H "Content-Type: application/json" \
  -d '{"message": "Summarize the key points from my document"}'
```

<br/>

## Makefile Reference

```
make setup          # First-time setup (copy .env, build images)
make up             # Start all services + pull Ollama models
make up-build       # Rebuild images and start
make down           # Stop all services
make restart        # Restart all services
make status         # Show container health status
make logs           # Tail all logs
make logs-api       # Tail API logs only
make logs-ai        # Tail AI engine logs only
make smoke          # Quick end-to-end health check
make test           # Run C# + Python tests
make test-api       # C# tests only (xUnit)
make test-ai        # Python tests only (pytest)
make test-security  # Security test suite
make bench          # Performance benchmarks
make eval           # AI quality evaluation
make pull-models    # Manually pull Ollama models
make list-models    # List loaded Ollama models
make shell-api      # Bash shell in API container
make shell-ai       # Bash shell in AI engine container
make shell-db       # psql in PostgreSQL container
make shell-redis    # redis-cli
make clean          # Remove containers + images (keep data)
make nuke           # Remove everything including volumes
```

<br/>

## Deployment

### Azure (Terraform)

```bash
cd infrastructure/terraform
terraform init
terraform plan -var-file=environments/dev/terraform.tfvars
terraform apply
```

Estimated cost: **~$18/mo** on Azure free tier (first 12 months), ~$34/mo after.

### CI/CD

Three GitHub Actions workflows:
- **CI** — Build + test all 3 projects on every push
- **Deploy** — Build Docker images → push to ACR → Terraform apply → deploy to App Service
- **Benchmark** — Run performance benchmarks, upload results as artifacts

<br/>

## Documentation

| Document | Description |
|----------|-------------|
| [Architecture](docs/architecture.md) | C4 diagrams, data flows, security, scalability |
| [ADR-001](docs/adr/001-dotnet-api.md) | Why .NET 9 |
| [ADR-002](docs/adr/002-qdrant-vector-db.md) | Why Qdrant |
| [ADR-003](docs/adr/003-hybrid-search.md) | Hybrid search with RRF |
| [ADR-004](docs/adr/004-citation-verification.md) | Citation verification design |
| [ADR-005](docs/adr/005-prompt-versioning.md) | Prompt versioning |
| [Production Readiness](docs/production-readiness.md) | Checklist |
| [Cost Estimation](docs/azure-cost-estimation.md) | Azure cost analysis |

<br/>

## License

MIT
