from fastapi import APIRouter

from app.evaluation.evaluator import run_evaluation, load_golden_dataset
from app.models.schemas import EvalSummary

router = APIRouter()


@router.post("/run", response_model=EvalSummary)
async def run_eval(
    tenant_id: str = "a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11",
    model: str | None = None,
    tags: list[str] | None = None,
):
    return await run_evaluation(tenant_id, model=model, tags=tags)


@router.get("/dataset")
async def get_dataset():
    cases = load_golden_dataset()
    return {"total": len(cases), "cases": [c.model_dump() for c in cases]}
