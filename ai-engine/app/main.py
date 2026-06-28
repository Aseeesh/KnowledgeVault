import logging
from contextlib import asynccontextmanager

from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import PlainTextResponse
from prometheus_client import generate_latest, CONTENT_TYPE_LATEST

from app.clients.ollama import ollama_client
from app.core.config import settings
from app.models.schemas import HealthResponse
from app.prompts.manager import load_templates
from app.routers import embeddings, search, generation, models, evaluation, prompts
from app.services.vector_store import vector_store

logging.basicConfig(level=settings.log_level, format="%(asctime)s %(levelname)s %(name)s: %(message)s")
logger = logging.getLogger(__name__)


@asynccontextmanager
async def lifespan(app: FastAPI):
    logger.info("Starting KnowledgeVault AI Engine...")
    load_templates()
    await vector_store.ensure_collection()
    logger.info("AI Engine ready (model=%s, embedding=%s)", settings.llm_model, settings.embedding_model)
    yield
    await ollama_client.close()
    logger.info("AI Engine shutdown")


app = FastAPI(
    title="KnowledgeVault AI Engine",
    version="2.0.0",
    description="RAG pipeline with hybrid search, citation verification, and prompt evaluation",
    lifespan=lifespan,
)

app.add_middleware(CORSMiddleware, allow_origins=["*"], allow_methods=["*"], allow_headers=["*"])

app.include_router(embeddings.router, prefix="/embeddings", tags=["Embeddings"])
app.include_router(search.router, prefix="/search", tags=["Search"])
app.include_router(generation.router, prefix="/generate", tags=["Generation"])
app.include_router(models.router, prefix="/models", tags=["Models"])
app.include_router(evaluation.router, prefix="/eval", tags=["Evaluation"])
app.include_router(prompts.router, prefix="/prompts", tags=["Prompts"])


@app.get("/health", response_model=HealthResponse)
async def health():
    ollama_ok = False
    models_available = []
    try:
        model_list = await ollama_client.list_models()
        ollama_ok = True
        models_available = [m.get("name", "") for m in model_list]
    except Exception:
        pass

    qdrant_ok = False
    try:
        vector_store.collection_info()
        qdrant_ok = True
    except Exception:
        pass

    redis_ok = False
    try:
        from app.services.embedding import get_redis
        r = await get_redis()
        await r.ping()
        redis_ok = True
    except Exception:
        pass

    return HealthResponse(
        status="healthy" if ollama_ok else "degraded",
        service="ai-engine",
        ollama_connected=ollama_ok,
        qdrant_connected=qdrant_ok,
        redis_connected=redis_ok,
        models_available=models_available,
    )


@app.get("/metrics")
async def prometheus_metrics():
    return PlainTextResponse(generate_latest(), media_type=CONTENT_TYPE_LATEST)
