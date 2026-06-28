import hashlib
import json
import logging

import redis.asyncio as redis

from app.clients.ollama import ollama_client
from app.core.config import settings
from app.metrics.collector import metrics

logger = logging.getLogger(__name__)

_redis: redis.Redis | None = None


async def get_redis() -> redis.Redis:
    global _redis
    if _redis is None:
        _redis = redis.from_url(settings.redis_url, decode_responses=True)
    return _redis


def _cache_key(text: str, model: str) -> str:
    h = hashlib.sha256(f"{model}:{text}".encode()).hexdigest()[:16]
    return f"emb:{model}:{h}"


async def embed_text(text: str, model: str | None = None) -> list[float]:
    model = model or settings.embedding_model
    key = _cache_key(text, model)

    r = await get_redis()
    cached = await r.get(key)
    if cached:
        metrics.embedding_cache_hits.inc()
        return json.loads(cached)

    metrics.embedding_cache_misses.inc()
    with metrics.embedding_latency.time():
        embedding = await ollama_client.embed(text, model)

    await r.set(key, json.dumps(embedding), ex=settings.embedding_cache_ttl)
    metrics.embeddings_generated.inc()
    return embedding


async def embed_batch(texts: list[str], model: str | None = None) -> tuple[list[list[float]], int]:
    model = model or settings.embedding_model
    r = await get_redis()
    results: list[list[float] | None] = [None] * len(texts)
    uncached_indices: list[int] = []
    cached_count = 0

    for i, text in enumerate(texts):
        key = _cache_key(text, model)
        cached = await r.get(key)
        if cached:
            results[i] = json.loads(cached)
            cached_count += 1
            metrics.embedding_cache_hits.inc()
        else:
            uncached_indices.append(i)
            metrics.embedding_cache_misses.inc()

    if uncached_indices:
        uncached_texts = [texts[i] for i in uncached_indices]
        logger.info("Generating %d embeddings (batch size %d, %d cached)", len(uncached_texts), settings.chunk_batch_size, cached_count)

        with metrics.embedding_latency.time():
            new_embeddings = await ollama_client.embed_batch(uncached_texts, model)

        pipe = r.pipeline()
        for idx, emb in zip(uncached_indices, new_embeddings):
            results[idx] = emb
            key = _cache_key(texts[idx], model)
            pipe.set(key, json.dumps(emb), ex=settings.embedding_cache_ttl)
        await pipe.execute()

        metrics.embeddings_generated.inc(len(new_embeddings))

    return [r for r in results if r is not None], cached_count
