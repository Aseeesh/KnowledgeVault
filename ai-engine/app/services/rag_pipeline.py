import logging
import re
import time

from app.clients.ollama import ollama_client
from app.core.config import settings
from app.metrics.collector import metrics
from app.models.schemas import Citation, RAGResponse, SearchHit
from app.prompts.manager import build_rag_prompt
from app.services.hybrid_search import hybrid_search
from app.services.token_counter import count_tokens, fits_context

logger = logging.getLogger(__name__)


async def rag_generate(
    tenant_id: str,
    query: str,
    top_k: int = 5,
    model: str | None = None,
    session_history: list[dict[str, str]] | None = None,
) -> RAGResponse:
    start = time.time()

    search_result = await hybrid_search(tenant_id, query, top_k=top_k)

    chunks = [
        {"content": h.content, "document_title": h.document_title, "chunk_id": h.chunk_id}
        for h in search_result.hits
    ]

    chunk_texts = [c["content"] for c in chunks]
    fitting_texts, context_tokens = fits_context(chunk_texts, settings.max_context_tokens)
    chunks = chunks[: len(fitting_texts)]

    system, prompt = build_rag_prompt(query, chunks, session_history)

    prompt_tokens = count_tokens(prompt) + count_tokens(system)
    logger.info("RAG prompt: %d context tokens, %d prompt tokens, %d chunks", context_tokens, prompt_tokens, len(chunks))

    with metrics.generation_latency.time():
        result = await ollama_client.generate(
            prompt=prompt,
            model=model,
            system=system,
            max_tokens=settings.max_response_tokens,
        )

    answer = result["response"]
    tokens_used = result.get("eval_count", 0) + result.get("prompt_eval_count", 0)

    citations = extract_citations(answer, chunks)
    confidence = compute_confidence(citations, chunks, answer)

    elapsed = (time.time() - start) * 1000
    metrics.rag_requests.inc()

    logger.info(
        "RAG complete: %.0fms, %d tokens, %d citations, confidence=%.2f",
        elapsed, tokens_used, len(citations), confidence,
    )

    return RAGResponse(
        answer=answer,
        citations=citations,
        search_results=search_result.hits,
        model=result.get("model", model or settings.llm_model),
        tokens_used=tokens_used,
        confidence_score=confidence,
        response_time_ms=elapsed,
    )


def extract_citations(answer: str, chunks: list[dict]) -> list[Citation]:
    citations = []
    seen = set()

    for match in re.finditer(r"\[Source\s+(\d+)\]", answer):
        idx = int(match.group(1)) - 1
        if 0 <= idx < len(chunks) and idx not in seen:
            seen.add(idx)
            chunk = chunks[idx]
            content = chunk["content"]
            citations.append(
                Citation(
                    chunk_index=idx,
                    text=content[:200] + "..." if len(content) > 200 else content,
                    confidence=0.8,
                )
            )

    if not citations and chunks:
        for i, chunk in enumerate(chunks[:2]):
            content = chunk["content"]
            answer_words = set(answer.lower().split())
            chunk_words = set(content.lower().split())
            overlap = len(answer_words & chunk_words) / max(len(answer_words), 1)
            if overlap > 0.15:
                citations.append(
                    Citation(
                        chunk_index=i,
                        text=content[:200] + "..." if len(content) > 200 else content,
                        confidence=round(min(overlap * 2, 0.7), 2),
                    )
                )

    return citations


def compute_confidence(citations: list[Citation], chunks: list[dict], answer: str) -> float:
    if not chunks:
        return 0.0

    citation_coverage = len(citations) / max(len(chunks), 1)
    avg_citation_confidence = sum(c.confidence for c in citations) / max(len(citations), 1) if citations else 0

    answer_length_score = min(len(answer.split()) / 50, 1.0)
    has_hedging = any(
        phrase in answer.lower()
        for phrase in ["i cannot", "i'm not sure", "based on the available", "insufficient"]
    )
    hedging_penalty = 0.2 if has_hedging else 0

    confidence = (
        0.4 * avg_citation_confidence
        + 0.3 * citation_coverage
        + 0.2 * answer_length_score
        - hedging_penalty
    )
    return round(max(0.0, min(1.0, confidence)), 2)
