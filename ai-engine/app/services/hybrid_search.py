import logging
import time

import numpy as np
from rank_bm25 import BM25Okapi

from app.core.config import settings
from app.metrics.collector import metrics
from app.models.schemas import SearchHit, SearchResponse
from app.services.embedding import embed_text
from app.services.vector_store import vector_store

logger = logging.getLogger(__name__)


async def hybrid_search(
    tenant_id: str,
    query: str,
    top_k: int = 10,
    dense_weight: float = 0.5,
    sparse_weight: float = 0.5,
    filters: dict[str, str] | None = None,
) -> SearchResponse:
    start = time.time()

    query_vector = await embed_text(query)
    dense_results = vector_store.search(query_vector, tenant_id, top_k=top_k * 3, filters=filters)

    sparse_results = _bm25_rerank(dense_results, query, top_k * 3)
    fused = _reciprocal_rank_fusion(dense_results, sparse_results, dense_weight, sparse_weight)
    final = fused[:top_k]

    elapsed = (time.time() - start) * 1000
    metrics.search_latency.observe(elapsed / 1000)
    metrics.search_requests.inc()

    hits = [
        SearchHit(
            chunk_id=r["chunk_id"],
            content=r["content"],
            score=r["fused_score"],
            source="hybrid",
            document_title=r.get("document_title", ""),
            section_heading=r.get("section_heading", ""),
            dense_score=r.get("dense_score", 0),
            sparse_score=r.get("sparse_score", 0),
        )
        for r in final
    ]

    logger.info(
        "Hybrid search '%s': %d results in %.0fms (dense=%d, sparse=%d)",
        query[:50], len(hits), elapsed, len(dense_results), len(sparse_results),
    )
    return SearchResponse(hits=hits, total=len(hits), response_time_ms=elapsed)


def _bm25_rerank(candidates: list[dict], query: str, top_k: int) -> list[dict]:
    if not candidates:
        return []
    tokenized = [doc["content"].lower().split() for doc in candidates]
    bm25 = BM25Okapi(tokenized)
    scores = bm25.get_scores(query.lower().split())
    ranked = np.argsort(scores)[::-1][:top_k]
    return [
        {**candidates[i], "bm25_score": float(scores[i])}
        for i in ranked
        if scores[i] > 0
    ]


def _reciprocal_rank_fusion(
    dense: list[dict], sparse: list[dict],
    dense_weight: float, sparse_weight: float,
    k: int = 60,
) -> list[dict]:
    scores: dict[str, dict] = {}

    for rank, r in enumerate(dense):
        cid = r["chunk_id"]
        if cid not in scores:
            scores[cid] = {**r, "dense_score": r.get("score", 0), "sparse_score": 0.0, "fused_score": 0.0}
        scores[cid]["fused_score"] += dense_weight / (k + rank + 1)
        scores[cid]["dense_score"] = r.get("score", 0)

    for rank, r in enumerate(sparse):
        cid = r["chunk_id"]
        if cid not in scores:
            scores[cid] = {**r, "dense_score": 0.0, "sparse_score": 0.0, "fused_score": 0.0}
        scores[cid]["fused_score"] += sparse_weight / (k + rank + 1)
        scores[cid]["sparse_score"] = r.get("bm25_score", 0)

    return sorted(scores.values(), key=lambda x: x["fused_score"], reverse=True)
