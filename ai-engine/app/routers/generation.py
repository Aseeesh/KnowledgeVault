from fastapi import APIRouter, HTTPException
from fastapi.responses import StreamingResponse

from app.clients.ollama import ollama_client
from app.core.config import settings
from app.models.schemas import GenerateRequest, GenerateResponse, RAGRequest, RAGResponse
from app.prompts.manager import build_rag_prompt, get_system_prompt
from app.services.rag_pipeline import extract_citations, compute_confidence, rag_generate

router = APIRouter()


@router.post("/", response_model=GenerateResponse)
async def generate_answer(request: GenerateRequest):
    chunks = [{"content": c, "document_title": f"Source {i+1}"} for i, c in enumerate(request.context_chunks)]
    system, prompt = build_rag_prompt(request.query, chunks, request.session_history)

    try:
        result = await ollama_client.generate(prompt=prompt, model=request.model, system=system)
    except Exception as e:
        raise HTTPException(502, f"Generation failed: {e}")

    answer = result["response"]
    citations = extract_citations(answer, chunks)
    confidence = compute_confidence(citations, chunks, answer)

    return GenerateResponse(
        answer=answer,
        citations=citations,
        model=result.get("model", request.model or settings.llm_model),
        tokens_used=result.get("eval_count", 0) + result.get("prompt_eval_count", 0),
        confidence_score=confidence,
    )


@router.post("/stream")
async def generate_stream(request: GenerateRequest):
    chunks = [{"content": c, "document_title": f"Source {i+1}"} for i, c in enumerate(request.context_chunks)]
    system, prompt = build_rag_prompt(request.query, chunks, request.session_history)

    async def event_stream():
        try:
            async for token in ollama_client.generate_stream(prompt=prompt, model=request.model, system=system):
                yield f"data: {token}\n\n"
            yield "data: [DONE]\n\n"
        except Exception as e:
            yield f"data: [ERROR] {e}\n\n"

    return StreamingResponse(event_stream(), media_type="text/event-stream")


@router.post("/rag", response_model=RAGResponse)
async def rag_endpoint(request: RAGRequest):
    try:
        return await rag_generate(
            tenant_id=request.tenant_id,
            query=request.query,
            top_k=request.top_k,
            model=request.model,
            session_history=request.session_history,
        )
    except Exception as e:
        raise HTTPException(502, f"RAG pipeline failed: {e}")
