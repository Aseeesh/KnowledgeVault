from fastapi import APIRouter, HTTPException

from app.models.schemas import EmbeddingRequest, EmbeddingResponse
from app.services.embedding import embed_batch

router = APIRouter()


@router.post("/", response_model=EmbeddingResponse)
async def create_embeddings(request: EmbeddingRequest):
    try:
        embeddings, cached_count = await embed_batch(request.texts, request.model)
    except Exception as e:
        raise HTTPException(502, f"Embedding generation failed: {e}")

    return EmbeddingResponse(
        embeddings=embeddings,
        model=request.model or "nomic-embed-text",
        dimensions=len(embeddings[0]) if embeddings else 0,
        cached=cached_count,
    )
