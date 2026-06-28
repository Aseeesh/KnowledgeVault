from fastapi import APIRouter

from app.models.schemas import SearchRequest, SearchResponse
from app.services.hybrid_search import hybrid_search

router = APIRouter()


@router.post("/hybrid", response_model=SearchResponse)
async def search(request: SearchRequest):
    return await hybrid_search(
        tenant_id=request.tenant_id,
        query=request.query,
        top_k=request.top_k,
        filters=request.filters,
    )
