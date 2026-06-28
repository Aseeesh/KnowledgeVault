import logging

from qdrant_client import QdrantClient
from qdrant_client.models import (
    Distance, VectorParams, PointStruct,
    Filter, FieldCondition, MatchValue,
)

from app.core.config import settings

logger = logging.getLogger(__name__)


class VectorStoreService:
    def __init__(self):
        self.client = QdrantClient(
            host=settings.qdrant_host,
            port=settings.qdrant_http_port,
            grpc_port=settings.qdrant_grpc_port,
            prefer_grpc=False,
        )
        self.collection = settings.collection_name

    async def ensure_collection(self, vector_size: int | None = None):
        vector_size = vector_size or settings.embedding_dimensions
        collections = self.client.get_collections().collections
        if not any(c.name == self.collection for c in collections):
            self.client.create_collection(
                collection_name=self.collection,
                vectors_config=VectorParams(size=vector_size, distance=Distance.COSINE),
            )
            logger.info("Created collection '%s' (dim=%d)", self.collection, vector_size)

    def upsert(self, points: list[dict]):
        self.client.upsert(
            collection_name=self.collection,
            points=[
                PointStruct(id=p["id"], vector=p["vector"], payload=p["payload"])
                for p in points
            ],
        )

    def search(
        self,
        query_vector: list[float],
        tenant_id: str,
        top_k: int = 20,
        filters: dict[str, str] | None = None,
    ) -> list[dict]:
        must_conditions = [
            FieldCondition(key="tenant_id", match=MatchValue(value=tenant_id))
        ]
        if filters:
            for key, value in filters.items():
                must_conditions.append(FieldCondition(key=key, match=MatchValue(value=value)))

        results = self.client.query_points(
            collection_name=self.collection,
            query=query_vector,
            query_filter=Filter(must=must_conditions),
            limit=top_k,
        )
        return [
            {
                "chunk_id": str(r.id),
                "content": r.payload.get("content", ""),
                "score": r.score,
                "document_title": r.payload.get("document_title", ""),
                "document_id": r.payload.get("document_id", ""),
                "section_heading": r.payload.get("section_heading", ""),
                "chunk_index": r.payload.get("chunk_index", 0),
            }
            for r in results.points
        ]

    def delete_by_document(self, document_id: str):
        self.client.delete(
            collection_name=self.collection,
            points_selector=Filter(
                must=[FieldCondition(key="document_id", match=MatchValue(value=document_id))]
            ),
        )

    def collection_info(self) -> dict:
        try:
            info = self.client.get_collection(self.collection)
            return {"points_count": info.points_count, "status": str(info.status)}
        except Exception:
            return {"points_count": 0, "status": "unknown"}


vector_store = VectorStoreService()
