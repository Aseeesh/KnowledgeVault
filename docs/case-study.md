# Building a Production RAG System from Scratch

*A technical case study on KnowledgeVault — an end-to-end Retrieval-Augmented Generation platform*

---

## The Problem

Organizations accumulate knowledge across hundreds of documents — technical specs, policy manuals, research papers, onboarding guides. When someone needs an answer, they either search through files manually, ask a colleague who might remember, or throw the question at a generic LLM that hallucinates confidently.

The core challenge: **How do you build an AI system that answers questions from your specific documents, cites its sources, and tells you when it's not sure?**

This isn't a wrapper around ChatGPT. It's a ground-up system that runs locally, isolates tenant data, verifies every citation, and costs under $20/month to host.

---

## The Solution

KnowledgeVault is a microservices RAG platform with three custom applications and six infrastructure services:

**The pipeline:**
1. User uploads a document (PDF, DOCX, TXT)
2. The system chunks it into overlapping segments, generates embeddings via Ollama, and indexes them in Qdrant
3. When a user asks a question, the system runs hybrid search (dense vector + BM25), builds a context prompt from the top results, and generates an answer with citations
4. Every citation is verified against the source text. A confidence score is computed. Potential hallucinations are flagged.

**Key architectural decisions:**

- **Hybrid search over pure vector search** — Dense vectors miss exact keyword matches. BM25 misses semantic similarity. Reciprocal Rank Fusion merges both rankings without needing score normalization. Result: 35ms P50 search latency.

- **Citation verification without a second LLM call** — Instead of asking another LLM "is this citation correct?", we embed both the claim and the source chunk and compute cosine similarity. Threshold of 0.7 = verified, 0.5 = plausible, below = unsupported. This adds ~100ms, not 2-3 seconds.

- **Multi-tenant at the data layer** — Every PostgreSQL query, every Qdrant search, every Redis cache key is scoped to a tenant ID. There's no way to accidentally leak data across tenants because the filter is in the repository layer, not the controller.

---

## Technical Deep Dive

### Hybrid Search: Why RRF Beats Linear Combination

The naive approach to hybrid search is: `final_score = α × dense_score + (1-α) × sparse_score`. This breaks because dense scores (cosine similarity, 0-1) and sparse scores (BM25, 0-∞) are on completely different scales. You'd need score normalization, which introduces its own problems.

Reciprocal Rank Fusion is rank-based:

```
RRF_score(d) = Σ w_i / (k + rank_i(d))
```

It doesn't care about score magnitudes — only relative ordering. A document ranked #1 by dense search and #3 by BM25 gets a higher fused score than one ranked #5 by both. The constant k=60 prevents the top-1 result from dominating.

We also make the weights per-tenant configurable. A legal team searching contracts might want 70% BM25 weight (exact terms matter). A research team searching papers might want 70% dense weight (semantic similarity matters).

### Citation Verification: The Claim Extraction Problem

The LLM generates an answer like: *"RAG combines retrieval with generation [Source 1]. It was first proposed in 2020 [Source 2]."*

Step 1: Extract `[Source N]` references via regex. Map back to retrieved chunks.

Step 2: For claims without explicit citations, we extract each sentence, embed it, and compute cosine similarity against all source chunk embeddings. If the best match scores above 0.5, we have a potential citation. Above 0.7, it's verified.

Step 3: If more than 30% of claims have no supporting source, we flag `hasPotentialHallucinations: true`. The frontend shows a warning badge.

The key insight: **we don't need a perfect extraction — we need a reliable signal**. A 0.71 confidence score with 7 verified citations tells the user "this answer is probably correct, and here's where it came from."

### Prompt Engineering: YAML Templates with Evaluation

Prompts are stored in versioned YAML files, not hardcoded strings:

```yaml
version: v1
system:
  rag_answer: |
    You are a knowledgeable assistant...
    RULES:
    1. Only use information from provided sources
    2. Cite using [Source N] format
    3. If insufficient info, say so
```

A golden dataset of 5 test cases evaluates each prompt version:
- Semantic similarity between generated and expected answers (embedding cosine)
- Citation precision and recall (did it cite the right chunks?)
- Latency regression detection

Current results: **85.5% average answer similarity**, **60% citation recall** across the golden dataset.

---

## Results

### Performance

| Metric | Value |
|--------|-------|
| Search latency (P50) | 35ms |
| RAG end-to-end (P50) | 2,487ms |
| Cache hit rate | 56% |
| Error rate | 0.0% |
| Answer similarity | 85.5% |

The 2.5-second RAG latency is 95%+ LLM inference on CPU. With GPU acceleration, this drops to ~300-500ms. With a cloud API like Groq, it's ~200ms.

### Cost

| Tier | Monthly |
|------|---------|
| Local development | $0 (Ollama + Docker) |
| Azure free tier | ~$18/mo |
| Azure post-free-tier | ~$34/mo |

### Test Coverage

- 35 C# unit/integration tests (xUnit)
- 46 Python tests (pytest)
- 16 security tests
- 5 AI quality evaluations
- k6 load test scripts (smoke, load, stress, spike scenarios)

---

## Lessons Learned

### What Worked Well

1. **Separating AI from API** — The Python AI engine and C# API are independent services. The API handles auth, CRUD, and orchestration. The AI engine handles embeddings, search, and generation. Either can be scaled or replaced independently.

2. **Redis caching for embeddings** — Embedding the same query text multiple times is wasteful. A 24-hour TTL cache with SHA256 key hashing reduced Ollama calls by 56% in benchmarks.

3. **MediatR for CQRS** — Commands and queries are separate classes with their own validators and handlers. Adding a new endpoint means adding a command/query file and a handler — no controller changes needed.

4. **Prompt versioning from day one** — Being able to reload prompts without restart and evaluate them against a golden dataset prevented several regressions during development.

### What Could Be Improved

1. **BM25 on dense results only** — Our BM25 reranks the top-N dense results rather than searching the full corpus. This means we miss documents that are keyword-relevant but not semantically similar. A proper BM25 index in PostgreSQL (using the `tsvector` column) would fix this.

2. **Cross-encoder reranking** — Currently a placeholder. A small cross-encoder model (MiniLM) reranking the top-20 results would significantly improve precision.

3. **Document upload pipeline** — Currently uses placeholder text. Needs actual PDF/DOCX parsing integration (e.g., Apache Tika or python-docx).

4. **JWT authentication** — API key auth works for demos but production needs JWT with refresh tokens and RBAC.

---

## What's Next

| Phase | Feature |
|-------|---------|
| v2.1 | Real document parsing (PDF, DOCX) |
| v2.1 | JWT authentication with RBAC |
| v2.2 | GPU-accelerated Ollama |
| v2.2 | Cross-encoder reranking |
| v2.3 | WebSocket streaming |
| v3.0 | Kubernetes deployment |
| v3.0 | Multi-model routing |

---

## Try It

```bash
ollama pull llama3.2:1b && ollama pull nomic-embed-text
git clone <repo> && cd KnowledgeVault
docker compose up --build
```

Open http://localhost:5173 and ask a question.

---

*Built with .NET 9, Python FastAPI, React 19, Qdrant, PostgreSQL, Redis, RabbitMQ, Ollama, Docker, Terraform, and GitHub Actions.*
