# ADR-002: Use Qdrant for Vector Database

**Status:** Accepted
**Date:** 2026-06-27

## Context

The RAG system needs a vector database for storing document chunk embeddings and performing approximate nearest neighbor (ANN) search filtered by tenant.

## Decision

Use Qdrant (self-hosted via Docker) for vector storage and search.

## Rationale

- **Zero cost**: Open-source, self-hosted — no per-query pricing
- **Payload filtering**: Tenant isolation via `tenant_id` payload field, applied during search — no separate collections per tenant needed
- **gRPC + REST**: Dual API, gRPC for performance-critical paths
- **Cosine similarity**: Built-in distance metric matching our embedding model
- **Simple operations**: Single binary, Docker-friendly, snapshot backups
- **Benchmarked at 35ms P50** for hybrid search with 5 vectors (will scale to millions)

## Alternatives Considered

- **Pinecone**: Managed but costs $70+/mo minimum, no self-hosted option
- **pgvector**: Free but ANN performance degrades beyond ~100K vectors without IVFFlat tuning
- **Weaviate**: Feature-rich but heavier resource footprint and GraphQL complexity
- **Milvus**: Powerful but operationally complex (requires etcd, MinIO)

## Consequences

- Must manage Qdrant instance (Docker container, volume persistence)
- No built-in full-text search — we complement with PostgreSQL `tsvector`
- Scaling beyond single node requires Qdrant Cloud or cluster mode
