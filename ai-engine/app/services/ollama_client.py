import httpx

from app.core.config import settings


class OllamaClient:
    def __init__(self):
        self.base_url = settings.ollama_base_url

    async def embed(self, text: str, model: str | None = None) -> list[float]:
        model = model or settings.embedding_model
        async with httpx.AsyncClient(timeout=120) as client:
            resp = await client.post(
                f"{self.base_url}/api/embeddings",
                json={"model": model, "prompt": text},
            )
            resp.raise_for_status()
            return resp.json()["embedding"]

    async def embed_batch(self, texts: list[str], model: str | None = None) -> list[list[float]]:
        return [await self.embed(text, model) for text in texts]

    async def generate(self, prompt: str, model: str | None = None) -> str:
        model = model or settings.llm_model
        async with httpx.AsyncClient(timeout=300) as client:
            resp = await client.post(
                f"{self.base_url}/api/generate",
                json={"model": model, "prompt": prompt, "stream": False},
            )
            resp.raise_for_status()
            return resp.json()["response"]

    async def list_models(self) -> list[dict]:
        async with httpx.AsyncClient(timeout=10) as client:
            resp = await client.get(f"{self.base_url}/api/tags")
            resp.raise_for_status()
            return resp.json().get("models", [])
