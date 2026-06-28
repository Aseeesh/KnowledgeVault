# KnowledgeVault — Architecture Documentation

## 1. High-Level Architecture

### System Context (C4 Level 1)

```mermaid
graph TB
    U[/"👤 User<br/>Knowledge worker"/]
    A[/"👤 Admin<br/>System administrator"/]

    KV["🏢 KnowledgeVault<br/>RAG Platform<br/><i>Answers questions from<br/>uploaded documents with<br/>verified citations</i>"]

    OL["🤖 Ollama<br/><i>Local LLM inference<br/>llama3.2 + nomic-embed-text</i>"]

    AZ["☁️ Azure Cloud<br/><i>Hosting, storage,<br/>monitoring</i>"]

    U -->|"Ask questions,<br/>upload documents"| KV
    A -->|"Monitor, configure,<br/>manage tenants"| KV
    KV -->|"Generate embeddings,<br/>LLM inference"| OL
    KV -->|"Deploy, store,<br/>monitor"| AZ
```

### Container Diagram (C4 Level 2)

```mermaid
graph TB
    subgraph "Client Tier"
        WEB["📱 Web Client<br/><i>React 19 + TypeScript + Vite</i><br/>Chat, Search, Documents, Dashboard"]
    end

    subgraph "Gateway Tier"
        NG["🔀 Nginx Gateway<br/><i>Reverse proxy, TLS, routing</i><br/>:80"]
    end

    subgraph "Application Tier"
        API["⚙️ REST API<br/><i>C# .NET 9</i><br/>MediatR CQRS, Multi-tenant<br/>:5001"]
        AI["🧠 AI Engine<br/><i>Python FastAPI</i><br/>RAG Pipeline, Embeddings<br/>:8000"]
    end

    subgraph "Data Tier"
        PG["🗄️ PostgreSQL 16<br/><i>Documents, tenants,<br/>chat history, full-text search</i>"]
        QD["📐 Qdrant<br/><i>Dense vector storage<br/>ANN search</i>"]
        RD["⚡ Redis 7<br/><i>Embedding cache,<br/>search result cache</i>"]
        RMQ["📨 RabbitMQ<br/><i>Async ingestion queue<br/>Dead-letter handling</i>"]
    end

    subgraph "AI Tier"
        OL["🤖 Ollama<br/><i>llama3.2:1b (generation)<br/>nomic-embed-text (embeddings)</i>"]
    end

    WEB -->|HTTPS| NG
    NG -->|/api/*| API
    NG -->|/ai/*| AI
    NG -->|/*| WEB

    API -->|REST| AI
    API -->|EF Core| PG
    API -->|Cache| RD
    API -->|Publish jobs| RMQ

    AI -->|Embed + Generate| OL
    AI -->|Vector search| QD
    AI -->|Cache embeddings| RD
```

### Component Diagram — API (C4 Level 3)

```mermaid
graph TB
    subgraph "KnowledgeVault.API"
        MW["Middleware<br/><i>Security Headers<br/>Tenant Resolution<br/>Rate Limiting<br/>Request Logging<br/>Exception Handling</i>"]
        CT["Controllers (V1)<br/><i>Documents<br/>Search<br/>Chat<br/>Ingestion</i>"]
        BG["Background Services<br/><i>Ingestion Worker<br/>Freshness Monitor</i>"]
    end

    subgraph "KnowledgeVault.Application"
        CMD["Commands<br/><i>CreateDocument<br/>DeleteDocument<br/>ChatCompletion</i>"]
        QRY["Queries<br/><i>GetDocuments<br/>HybridSearch</i>"]
        SVC["Services<br/><i>HybridSearchService<br/>CitationVerificationService<br/>DocumentIngestionService<br/>ChunkingService</i>"]
        VAL["Validators<br/><i>FluentValidation<br/>CreateDocument<br/>Search<br/>Chat</i>"]
        BEH["Behaviors<br/><i>ValidationBehavior<br/>LoggingBehavior</i>"]
    end

    subgraph "KnowledgeVault.Infrastructure"
        REP["Repositories<br/><i>DocumentRepository<br/>ChatRepository<br/>TenantRepository<br/>IngestionJobRepository</i>"]
        EXT["External Services<br/><i>RedisCacheService<br/>QdrantVectorStore<br/>EmbeddingService<br/>RabbitMqService</i>"]
        DB["AppDbContext<br/><i>EF Core + PostgreSQL</i>"]
    end

    subgraph "KnowledgeVault.Core"
        ENT["Entities<br/><i>Tenant, Document,<br/>DocumentChunk, ChatSession,<br/>ChatMessage, IngestionJob</i>"]
        INT["Interfaces<br/><i>IDocumentRepository<br/>ISearchService<br/>ICitationService<br/>IIngestionService</i>"]
    end

    MW --> CT
    CT -->|MediatR| CMD
    CT -->|MediatR| QRY
    CMD --> SVC
    QRY --> SVC
    CMD --> VAL
    BEH --> VAL
    SVC --> REP
    SVC --> EXT
    REP --> DB
    REP --> ENT
    SVC --> INT
```

### Component Diagram — AI Engine

```mermaid
graph TB
    subgraph "Routers"
        R1["embeddings/"]
        R2["search/hybrid"]
        R3["generate/"]
        R4["generate/rag"]
        R5["generate/stream"]
        R6["models/"]
        R7["eval/run"]
        R8["prompts/"]
        R9["metrics"]
    end

    subgraph "Services"
        EMB["EmbeddingService<br/><i>Batch embed, Redis cache</i>"]
        HS["HybridSearchService<br/><i>Dense + BM25 + RRF</i>"]
        RAG["RAGPipeline<br/><i>Search → Context → Generate<br/>→ Citations → Confidence</i>"]
        TC["TokenCounter<br/><i>tiktoken, budget management</i>"]
    end

    subgraph "Clients"
        OC["OllamaClient<br/><i>Connection pooling<br/>Retry w/ backoff<br/>Streaming</i>"]
    end

    subgraph "Prompt Management"
        PM["PromptManager<br/><i>YAML templates<br/>Version control</i>"]
        TF["templates.yaml<br/><i>System prompts<br/>Generation prompts<br/>Verification prompts</i>"]
    end

    subgraph "Evaluation"
        EV["Evaluator<br/><i>Golden dataset<br/>Similarity scoring<br/>Citation P/R</i>"]
    end

    subgraph "External"
        VS["VectorStoreService<br/><i>Qdrant client</i>"]
        MC["Prometheus Metrics<br/><i>Counters, Histograms</i>"]
    end

    R4 --> RAG
    RAG --> HS
    RAG --> EMB
    RAG --> PM
    RAG --> TC
    HS --> VS
    EMB --> OC
    RAG --> OC
    R7 --> EV
    EV --> RAG
    PM --> TF
```

### Deployment Diagram

```mermaid
graph TB
    subgraph "Azure Cloud"
        subgraph "App Service Plan (F1)"
            API_APP["API Container<br/>.NET 9"]
        end
        subgraph "App Service Plan (B1)"
            AI_APP["AI Engine Container<br/>Python FastAPI"]
        end
        SWA["Static Web App<br/>React (CDN)"]
        PG_AZ["Azure PostgreSQL<br/>Flexible Server"]
        RD_AZ["Azure Redis<br/>Cache"]
        STR["Azure Storage<br/>Blob + Queue"]
        ACR["Container Registry"]
        MON["Application Insights<br/>+ Log Analytics"]
    end

    subgraph "Local Dev (Docker Compose)"
        DC["9 containers<br/>All services local"]
    end

    subgraph "CI/CD"
        GH["GitHub Actions<br/>Build → Test → Deploy"]
    end

    GH -->|Push images| ACR
    ACR -->|Pull| API_APP
    ACR -->|Pull| AI_APP
    GH -->|Terraform| PG_AZ
    GH -->|Terraform| RD_AZ
```

---

## 2. Technology Decisions

### ADR-001: C# .NET 9 for API

| Factor | .NET 9 | Node.js/Express | Go | Python/Django |
|--------|--------|----------------|----|----|
| Performance | Excellent | Good | Excellent | Moderate |
| Type safety | Strong (C#) | Weak (JS) / Good (TS) | Strong | Weak |
| ORM | EF Core (mature) | Prisma/TypeORM | GORM | Django ORM |
| CQRS/MediatR | Native ecosystem | Limited | Manual | Limited |
| Enterprise patterns | Excellent | Moderate | Minimal | Good |
| Async/await | Native | Native | Goroutines | asyncio |

**Decision:** .NET 9 for its mature enterprise patterns (MediatR, FluentValidation, EF Core), strong typing, excellent async support, and production-grade middleware pipeline.

### ADR-002: Qdrant for Vector Database

| Factor | Qdrant | Pinecone | Weaviate | Milvus | pgvector |
|--------|--------|---------|----------|--------|----------|
| Self-hosted | Yes | No | Yes | Yes | Yes |
| gRPC support | Yes | No | Yes | Yes | No |
| Filtering | Advanced | Basic | GraphQL | Advanced | SQL |
| Cost | Free (OSS) | $70+/mo | Free (OSS) | Free (OSS) | Free |
| Ease of use | High | High | Medium | Low | High |
| Tenant isolation | Payload filter | Namespace | Multi-tenancy | Partition | Row-level |

**Decision:** Qdrant for its zero-cost self-hosting, excellent filtering (tenant isolation via payload), gRPC performance, and simple API. pgvector was considered but lacks ANN performance at scale.

### ADR-003: React 19 for Frontend

**Decision:** React 19 with Vite for fast builds, TypeScript for type safety, Tailwind CSS for rapid UI development, Zustand for lightweight state management, and React Query for server state caching.

### ADR-004: RabbitMQ vs Kafka

| Factor | RabbitMQ | Kafka | Azure Queue |
|--------|---------|-------|-------------|
| Use case | Task queue | Event streaming | Simple queue |
| Complexity | Low | High | Very low |
| Dead-letter | Built-in | Manual | Built-in |
| Ordering | Per-queue | Per-partition | FIFO optional |
| Scale needed | Low-medium | High | Low |

**Decision:** RabbitMQ for document ingestion — we need reliable task delivery with dead-letter handling, not event streaming. Simpler to operate. Azure Storage Queue used in cloud deployment for cost savings.

---

## 3. Data Flow

### Chat Request Flow

```mermaid
sequenceDiagram
    participant U as User
    participant W as Web Client
    participant N as Nginx
    participant A as API (.NET)
    participant AI as AI Engine
    participant Q as Qdrant
    participant O as Ollama
    participant R as Redis
    participant P as PostgreSQL

    U->>W: Type question
    W->>N: POST /api/v1/chat/completions
    N->>A: Forward request
    A->>A: Tenant resolution (header/API key)
    A->>A: Rate limit check
    A->>A: FluentValidation
    A->>P: Create/load ChatSession
    A->>P: Save user message
    A->>AI: POST /generate/rag

    AI->>R: Check embedding cache
    alt Cache miss
        AI->>O: POST /api/embeddings
        O-->>AI: Query vector [768d]
        AI->>R: Cache embedding (24hr TTL)
    end

    AI->>Q: Dense vector search (tenant filtered)
    AI->>AI: BM25 rerank on dense results
    AI->>AI: Reciprocal Rank Fusion
    AI->>AI: Token budget check
    AI->>AI: Build prompt from YAML template
    AI->>O: POST /api/generate
    O-->>AI: Generated answer
    AI->>AI: Extract [Source N] citations
    AI->>AI: Compute confidence score

    AI-->>A: RAGResponse

    A->>A: Citation verification (embedding similarity)
    A->>A: Hallucination detection
    A->>P: Save assistant message + citations
    A-->>N: ChatCompletionResponse
    N-->>W: JSON response
    W-->>U: Render answer + citation badges
```

### Document Ingestion Flow

```mermaid
sequenceDiagram
    participant U as User
    participant A as API
    participant MQ as RabbitMQ
    participant W as Ingestion Worker
    participant AI as AI Engine
    participant Q as Qdrant
    participant P as PostgreSQL

    U->>A: POST /api/v1/documents/upload
    A->>P: Create document (status: Pending)
    A->>P: Create IngestionJob (status: Queued)
    A->>MQ: Publish DocumentIngestionEvent
    A-->>U: 202 Accepted

    loop Every 5 seconds
        W->>P: Poll for Queued jobs
    end

    W->>P: Update job (Processing)
    W->>P: Update document (Processing)
    W->>W: Extract text from file
    W->>W: Detect sections/headings
    W->>W: Chunk with overlap
    W->>P: Bulk insert DocumentChunks
    W->>AI: Batch embed chunks
    W->>Q: Upsert vectors (tenant-scoped)
    W->>P: Update document (Indexed, chunk_count)
    W->>P: Update job (Completed)

    alt Failure
        W->>P: Increment retry_count
        W->>P: Update job (Failed/DeadLettered)
        W->>P: Update document (Failed, error_message)
    end
```

### Hybrid Search Flow

```mermaid
graph LR
    Q["Query"] --> E["Embed<br/>(nomic-embed-text)"]
    E --> C{"Redis<br/>cache?"}
    C -->|Hit| D
    C -->|Miss| O["Ollama"]
    O --> D["Dense Search<br/>(Qdrant, cosine)"]
    D --> B["BM25 Rerank<br/>(on dense results)"]
    B --> R["RRF Merge<br/>score = Σ w/(k+rank)"]
    D --> R
    R --> T["Token Budget<br/>Truncate to fit"]
    T --> F["Top-K Results"]

    style C fill:#f9f,stroke:#333
    style R fill:#ff9,stroke:#333
```

---

## 4. Security Architecture

### Authentication Flow

```mermaid
graph LR
    REQ["Request"] --> TM["TenantMiddleware"]
    TM --> H{"X-Tenant-Id<br/>header?"}
    H -->|Yes| VT["Validate tenant<br/>in PostgreSQL"]
    H -->|No| AK{"Bearer<br/>API key?"}
    AK -->|Yes| VA["Lookup by<br/>api_key"]
    AK -->|No| RT{"Route<br/>tenantId?"}
    RT -->|Yes| VR["Validate<br/>route param"]
    RT -->|No| R401["401<br/>Unauthorized"]

    VT -->|Active| RL["RateLimitMiddleware"]
    VA -->|Active| RL
    VR -->|Active| RL
    VT -->|Inactive| R401
    VA -->|Not found| R401

    RL --> RC{"Within<br/>limit?"}
    RC -->|Yes| CTRL["Controller"]
    RC -->|No| R429["429<br/>Too Many Requests"]
```

### Security Layers

| Layer | Implementation |
|-------|---------------|
| Transport | HTTPS/TLS 1.2+ (Azure enforced, HSTS header) |
| Headers | SecurityHeadersMiddleware: CSP, X-Frame-Options, nosniff, XSS-Protection |
| Authentication | API key + Tenant ID (extensible to JWT) |
| Authorization | Tenant isolation on all queries (EF Core filters) |
| Rate limiting | Per-tenant sliding window (configurable `rate_limit_per_minute`) |
| Input validation | FluentValidation on all MediatR commands/queries |
| SQL injection | EF Core parameterized queries (no raw SQL) |
| XSS | React auto-escaping, CSP headers |
| Request tracing | X-Request-Id header on every response |

---

## 5. Scalability Design

### Current Bottlenecks & Solutions

| Bottleneck | Impact | Solution | Cost |
|-----------|--------|----------|------|
| LLM inference (CPU) | 2.5s P50 RAG latency | GPU server / Cloud LLM API | $13-50/mo |
| Single API instance | Limited concurrent requests | Horizontal scaling (App Service B1 x2) | +$13/mo |
| Redis single node | 250MB cache limit | Azure Redis Standard (2.5GB) | +$25/mo |
| PostgreSQL single vCore | Query throughput ceiling | Read replicas | +$50/mo |

### Horizontal Scaling Strategy

```mermaid
graph TB
    LB["Load Balancer<br/>(Nginx / Azure Front Door)"]
    LB --> API1["API Instance 1"]
    LB --> API2["API Instance 2"]
    LB --> AI1["AI Engine 1"]
    LB --> AI2["AI Engine 2"]

    API1 --> PG["PostgreSQL<br/>(read replicas)"]
    API2 --> PG
    AI1 --> QD["Qdrant<br/>(cluster mode)"]
    AI2 --> QD
    API1 --> RD["Redis Cluster"]
    API2 --> RD
```

---

## 6. Operational Design

### Monitoring Strategy

| Metric | Source | Alert Threshold |
|--------|--------|----------------|
| HTTP 5xx rate | App Service | > 5 in 15 min |
| Response time P95 | App Insights | > 10 seconds |
| CPU utilization | Docker/Azure | > 80% for 5 min |
| Memory usage | Docker/Azure | > 90% |
| Queue depth | RabbitMQ | > 100 messages |
| Cache hit rate | Redis/Prometheus | < 30% |
| Embedding generation errors | Prometheus | > 5 in 10 min |
| Monthly cost | Azure Budget | > 80% of budget |

### Backup Strategy

| Data | Method | Frequency | Retention |
|------|--------|-----------|-----------|
| PostgreSQL | `pg_dump` / Azure auto-backup | Daily | 7 days |
| Qdrant | Snapshot API | Weekly | 30 days |
| Document blobs | Azure Storage geo-redundant | Continuous | 30 days |
| Redis | Not backed up (cache, reconstructible) | N/A | N/A |

---

## 7. Cost Optimization

### Current Monthly Cost (Azure Free Tier)

| Service | SKU | Cost |
|---------|-----|------|
| App Service (API) | F1 Free | $0 |
| App Service (AI) | B1 Basic | $13 |
| PostgreSQL Flexible | B1ms (free 12mo) | $0* |
| Redis Cache | Basic C0 | $16 |
| Container Registry | Basic | $5 |
| Static Web App | Free | $0 |
| Storage | Standard LRS | $0 |
| Application Insights | Free 5GB | $0 |
| **Total** | | **~$34/mo** |

*Free for first 12 months

### Optimization Strategies

1. **Use Ollama locally** for development, Groq free tier (30 req/min) for cloud demo
2. **Azure Storage Queue** replaces RabbitMQ in cloud ($0 vs $13/mo for CloudAMQP)
3. **Aggressive Redis caching** — 24hr embedding TTL, 5min search TTL reduces Ollama calls 50%+
4. **F1 tier API** — sufficient for portfolio demo traffic (60 CPU-min/day)

---

## 8. Future Roadmap

| Phase | Feature | Effort |
|-------|---------|--------|
| **v2.1** | JWT auth with refresh tokens | 1 week |
| **v2.1** | Document file upload (Azure Blob) | 1 week |
| **v2.2** | GPU-accelerated Ollama (CUDA) | 2 days |
| **v2.2** | Cross-encoder reranking (MiniLM) | 3 days |
| **v2.3** | WebSocket streaming for chat | 1 week |
| **v2.3** | Multi-language support | 2 weeks |
| **v3.0** | Kubernetes deployment (AKS) | 2 weeks |
| **v3.0** | Multi-model routing (Ollama + cloud) | 1 week |

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Ollama model unavailable | Medium | High | Fallback model config, health checks |
| Rate limit exhaustion | Low | Medium | Per-tenant configurable limits, 429 response |
| Cache poisoning | Low | Medium | TTL expiry, Redis auth, key hashing |
| Tenant data leakage | Low | Critical | EF Core tenant filter on every query |
| Cost overrun | Medium | Low | Azure Budget alerts at 80%/100% |
| LLM hallucination | High | Medium | Citation verification, confidence scoring, hedging detection |
