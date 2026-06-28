from fastapi import APIRouter, HTTPException
from fastapi.responses import StreamingResponse

from app.clients.ollama import ollama_client
from app.models.schemas import ModelInfo, ModelListResponse, PullModelRequest

router = APIRouter()


@router.get("/", response_model=ModelListResponse)
async def list_models():
    try:
        models = await ollama_client.list_models()
        return ModelListResponse(
            models=[
                ModelInfo(
                    name=m.get("name", ""),
                    size=m.get("size", 0),
                    modified_at=m.get("modified_at", ""),
                    digest=m.get("digest", ""),
                )
                for m in models
            ]
        )
    except Exception as e:
        raise HTTPException(502, f"Failed to list models: {e}")


@router.post("/pull")
async def pull_model(request: PullModelRequest):
    async def stream():
        async for line in ollama_client.pull_model(request.model):
            yield f"data: {line}\n\n"
        yield "data: [DONE]\n\n"

    return StreamingResponse(stream(), media_type="text/event-stream")


@router.delete("/{model_name}")
async def delete_model(model_name: str):
    success = await ollama_client.delete_model(model_name)
    if not success:
        raise HTTPException(404, f"Model '{model_name}' not found")
    return {"status": "deleted", "model": model_name}
