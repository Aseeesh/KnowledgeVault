from pydantic import BaseModel, Field


# ─── Embedding ──────────────────────────────────────────────────
class EmbeddingRequest(BaseModel):
    texts: list[str]
    model: str | None = None


class EmbeddingResponse(BaseModel):
    embeddings: list[list[float]]
    model: str
    dimensions: int
    cached: int = 0


# ─── Search ─────────────────────────────────────────────────────
class SearchRequest(BaseModel):
    tenant_id: str
    query: str
    top_k: int = Field(default=10, ge=1, le=100)
    filters: dict[str, str] | None = None


class SearchHit(BaseModel):
    chunk_id: str
    content: str
    score: float
    source: str = "hybrid"
    document_title: str = ""
    section_heading: str = ""
    dense_score: float = 0.0
    sparse_score: float = 0.0


class SearchResponse(BaseModel):
    hits: list[SearchHit]
    total: int
    response_time_ms: float = 0.0


# ─── Generation ─────────────────────────────────────────────────
class GenerateRequest(BaseModel):
    query: str
    context_chunks: list[str]
    model: str | None = None
    stream: bool = False
    session_history: list[dict[str, str]] | None = None


class Citation(BaseModel):
    chunk_index: int
    text: str
    confidence: float


class GenerateResponse(BaseModel):
    answer: str
    citations: list[Citation]
    model: str
    tokens_used: int = 0
    confidence_score: float = 0.0


# ─── RAG Pipeline ───────────────────────────────────────────────
class RAGRequest(BaseModel):
    tenant_id: str
    query: str
    top_k: int = 5
    model: str | None = None
    session_history: list[dict[str, str]] | None = None


class RAGResponse(BaseModel):
    answer: str
    citations: list[Citation]
    search_results: list[SearchHit]
    model: str
    tokens_used: int = 0
    confidence_score: float = 0.0
    response_time_ms: float = 0.0


# ─── Models ─────────────────────────────────────────────────────
class ModelInfo(BaseModel):
    name: str
    size: int = 0
    modified_at: str = ""
    digest: str = ""


class ModelListResponse(BaseModel):
    models: list[ModelInfo]


class PullModelRequest(BaseModel):
    model: str


# ─── Evaluation ─────────────────────────────────────────────────
class EvalCase(BaseModel):
    query: str
    expected_answer: str
    expected_citations: list[int] = []
    tags: list[str] = []


class EvalResult(BaseModel):
    query: str
    generated_answer: str
    expected_answer: str
    answer_similarity: float
    citation_precision: float
    citation_recall: float
    latency_ms: float
    passed: bool


class EvalSummary(BaseModel):
    total_cases: int
    passed: int
    failed: int
    avg_similarity: float
    avg_citation_precision: float
    avg_citation_recall: float
    avg_latency_ms: float
    prompt_version: str


# ─── Health ─────────────────────────────────────────────────────
class HealthResponse(BaseModel):
    status: str
    service: str
    ollama_connected: bool = False
    qdrant_connected: bool = False
    redis_connected: bool = False
    models_available: list[str] = []
