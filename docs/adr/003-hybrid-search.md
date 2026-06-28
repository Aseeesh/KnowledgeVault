# ADR-003: Hybrid Search with Reciprocal Rank Fusion

**Status:** Accepted
**Date:** 2026-06-27

## Context

Pure dense vector search misses keyword matches; pure BM25 misses semantic similarity. We need a search strategy that combines both for better recall.

## Decision

Implement hybrid search: dense vector search (Qdrant) + BM25 sparse reranking, merged via Reciprocal Rank Fusion (RRF) with per-tenant configurable weights.

## Algorithm

```
For each result d:
  RRF_score(d) = dense_weight × 1/(k + rank_dense(d)) + sparse_weight × 1/(k + rank_sparse(d))
  where k = 60 (standard RRF constant)
```

## Rationale

- **RRF is rank-based, not score-based** — works even when dense (cosine 0-1) and sparse (BM25 0-∞) scores are on different scales
- **No score normalization needed** — simpler and more robust than linear combination
- **Per-tenant weights** allow tuning: technical documentation might favor dense (semantic), legal docs might favor sparse (exact terms)
- **BM25 reranking on dense results** avoids maintaining a separate sparse index — we rerank the top-N dense results with BM25, then fuse

## Performance

- Dense search: 15ms (Qdrant)
- BM25 rerank: 2ms (in-memory on result set)
- RRF fusion: <1ms
- Total: **35ms P50** (benchmarked)

## Consequences

- BM25 only sees dense results (not full corpus) — may miss purely keyword-relevant documents
- PostgreSQL `tsvector` full-text search available as fallback for exact phrase matching
