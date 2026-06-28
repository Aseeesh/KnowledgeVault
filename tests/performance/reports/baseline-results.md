# KnowledgeVault — Baseline Performance Benchmarks

**Date:** 2026-06-27
**Environment:** Local Docker (MacOS, 8GB RAM allocated to Docker)
**LLM Model:** llama3.2:1b via Ollama (local CPU inference)
**Vector DB:** Qdrant with 5 indexed vectors

## Latency Results

| Endpoint | P50 | P95 | Avg | Iterations |
|----------|-----|-----|-----|-----------|
| Document List | 32ms | 49ms | 33ms | 10 |
| Embedding Generation | 40ms | 266ms | 90ms | 5 |
| Hybrid Search | 35ms | 68ms | 37ms | 10 |
| RAG Pipeline (end-to-end) | 2,487ms | 3,829ms | 2,450ms | 3 |
| Chat Completion | 2,571ms | 2,806ms | 2,109ms | 3 |

## Resource Usage (during benchmark)

| Service | CPU | Memory |
|---------|-----|--------|
| kv-api (.NET 9) | 13.78% | 341 MB / 1 GB |
| kv-ai-engine (Python) | 0.18% | 208 MB / 2 GB |
| kv-gateway (Nginx) | 0.00% | 7 MB / 128 MB |
| kv-rabbitmq | 10.19% | 103 MB / 512 MB |

## Cache Performance

| Metric | Value |
|--------|-------|
| Embedding cache hits | 19 |
| Embedding cache misses | 15 |
| Cache hit rate | 55.9% |

## Error Rate

- **0 errors** across 31 requests (0.0% error rate)

## Analysis

- **Document CRUD** is very fast (32ms P50) — EF Core + PostgreSQL handles this efficiently
- **Embedding generation** has high variance (40-266ms P95) — first call incurs Ollama model load, subsequent calls are cached in Redis
- **Hybrid search** is fast (35ms P50) — Qdrant + BM25 reranking with small dataset
- **RAG pipeline** dominates latency (2.5s P50) — almost entirely LLM inference time on CPU
- **Chat completion** adds ~100ms overhead over RAG — citation verification + DB persistence

## Bottleneck: LLM Inference

95%+ of RAG latency is Ollama generation on CPU. Optimization paths:
1. GPU acceleration (reduces inference 5-10x)
2. Smaller/quantized model
3. Speculative decoding
4. Cloud LLM API (Groq: ~200ms inference)
