import json
import logging
from collections.abc import AsyncIterator

import httpx
from tenacity import retry, stop_after_attempt, wait_exponential, retry_if_exception_type

from app.core.config import settings

logger = logging.getLogger(__name__)


class OllamaClient:
    def __init__(self):
        self._client: httpx.AsyncClient | None = None

    async def _get_client(self) -> httpx.AsyncClient:
        if self._client is None or self._client.is_closed:
            self._client = httpx.AsyncClient(
                base_url=settings.ollama_base_url,
                timeout=httpx.Timeout(
                    connect=settings.ollama_connect_timeout,
                    read=settings.ollama_timeout,
                    write=30.0,
                    pool=10.0,
                ),
                limits=httpx.Limits(max_connections=20, max_keepalive_connections=10),
            )
        return self._client

    async def close(self):
        if self._client and not self._client.is_closed:
            await self._client.aclose()

    # ─── Models ─────────────────────────────────────────────────
    async def list_models(self) -> list[dict]:
        client = await self._get_client()
        resp = await client.get("/api/tags")
        resp.raise_for_status()
        return resp.json().get("models", [])

    async def pull_model(self, model: str) -> AsyncIterator[str]:
        client = await self._get_client()
        async with client.stream("POST", "/api/pull", json={"name": model}) as resp:
            async for line in resp.aiter_lines():
                if line.strip():
                    yield line

    async def delete_model(self, model: str) -> bool:
        client = await self._get_client()
        resp = await client.request("DELETE", "/api/delete", json={"name": model})
        return resp.status_code == 200

    async def model_exists(self, model: str) -> bool:
        models = await self.list_models()
        return any(m.get("name", "").startswith(model) for m in models)

    # ─── Embeddings ─────────────────────────────────────────────
    @retry(
        stop=stop_after_attempt(settings.ollama_max_retries),
        wait=wait_exponential(multiplier=1, min=1, max=10),
        retry=retry_if_exception_type((httpx.HTTPStatusError, httpx.ConnectError)),
    )
    async def embed(self, text: str, model: str | None = None) -> list[float]:
        model = model or settings.embedding_model
        client = await self._get_client()
        resp = await client.post("/api/embeddings", json={"model": model, "prompt": text})
        resp.raise_for_status()
        return resp.json()["embedding"]

    async def embed_batch(self, texts: list[str], model: str | None = None) -> list[list[float]]:
        results = []
        for i in range(0, len(texts), settings.chunk_batch_size):
            batch = texts[i : i + settings.chunk_batch_size]
            batch_results = []
            for text in batch:
                try:
                    emb = await self.embed(text, model)
                    batch_results.append(emb)
                except Exception:
                    logger.warning("Embedding failed for text, trying fallback model")
                    emb = await self.embed(text, settings.fallback_embedding_model)
                    batch_results.append(emb)
            results.extend(batch_results)
        return results

    # ─── Generation ─────────────────────────────────────────────
    @retry(
        stop=stop_after_attempt(settings.ollama_max_retries),
        wait=wait_exponential(multiplier=1, min=2, max=30),
        retry=retry_if_exception_type((httpx.HTTPStatusError, httpx.ConnectError)),
    )
    async def generate(
        self,
        prompt: str,
        model: str | None = None,
        system: str | None = None,
        temperature: float = 0.3,
        max_tokens: int | None = None,
    ) -> dict:
        model = model or settings.llm_model
        client = await self._get_client()

        payload: dict = {
            "model": model,
            "prompt": prompt,
            "stream": False,
            "options": {"temperature": temperature},
        }
        if system:
            payload["system"] = system
        if max_tokens:
            payload["options"]["num_predict"] = max_tokens

        resp = await client.post("/api/generate", json=payload)
        resp.raise_for_status()
        data = resp.json()

        return {
            "response": data.get("response", ""),
            "model": data.get("model", model),
            "total_duration": data.get("total_duration", 0),
            "eval_count": data.get("eval_count", 0),
            "prompt_eval_count": data.get("prompt_eval_count", 0),
        }

    async def generate_stream(
        self,
        prompt: str,
        model: str | None = None,
        system: str | None = None,
        temperature: float = 0.3,
    ) -> AsyncIterator[str]:
        model = model or settings.llm_model
        client = await self._get_client()

        payload: dict = {
            "model": model,
            "prompt": prompt,
            "stream": True,
            "options": {"temperature": temperature},
        }
        if system:
            payload["system"] = system

        async with client.stream("POST", "/api/generate", json=payload) as resp:
            async for line in resp.aiter_lines():
                if not line.strip():
                    continue
                try:
                    data = json.loads(line)
                    token = data.get("response", "")
                    if token:
                        yield token
                    if data.get("done"):
                        return
                except json.JSONDecodeError:
                    continue

    async def chat(
        self,
        messages: list[dict[str, str]],
        model: str | None = None,
        temperature: float = 0.3,
    ) -> dict:
        model = model or settings.llm_model
        client = await self._get_client()

        resp = await client.post(
            "/api/chat",
            json={"model": model, "messages": messages, "stream": False, "options": {"temperature": temperature}},
        )
        resp.raise_for_status()
        data = resp.json()

        return {
            "response": data.get("message", {}).get("content", ""),
            "model": data.get("model", model),
            "eval_count": data.get("eval_count", 0),
        }


ollama_client = OllamaClient()
