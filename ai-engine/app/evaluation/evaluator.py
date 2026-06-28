import logging
import time
from pathlib import Path

import yaml

from app.models.schemas import EvalCase, EvalResult, EvalSummary
from app.prompts.manager import get_active_version
from app.services.embedding import embed_text
from app.services.rag_pipeline import rag_generate

logger = logging.getLogger(__name__)

EVAL_DIR = Path(__file__).parent


def load_golden_dataset(path: str | None = None) -> list[EvalCase]:
    path = path or str(EVAL_DIR / "golden_dataset.yaml")
    with open(path) as f:
        data = yaml.safe_load(f)
    return [EvalCase(**case) for case in data.get("cases", [])]


async def cosine_similarity(text_a: str, text_b: str) -> float:
    emb_a = await embed_text(text_a)
    emb_b = await embed_text(text_b)
    dot = sum(a * b for a, b in zip(emb_a, emb_b))
    mag_a = sum(a * a for a in emb_a) ** 0.5
    mag_b = sum(b * b for b in emb_b) ** 0.5
    return dot / (mag_a * mag_b + 1e-10)


async def evaluate_case(
    case: EvalCase, tenant_id: str, model: str | None = None,
) -> EvalResult:
    start = time.time()

    rag_result = await rag_generate(tenant_id, case.query, top_k=5, model=model)

    similarity = await cosine_similarity(rag_result.answer, case.expected_answer)

    generated_indices = {c.chunk_index for c in rag_result.citations}
    expected_indices = set(case.expected_citations)

    precision = (
        len(generated_indices & expected_indices) / len(generated_indices)
        if generated_indices
        else 0.0
    )
    recall = (
        len(generated_indices & expected_indices) / len(expected_indices)
        if expected_indices
        else 1.0
    )

    latency = (time.time() - start) * 1000
    passed = similarity >= 0.6 and (not expected_indices or recall >= 0.5)

    return EvalResult(
        query=case.query,
        generated_answer=rag_result.answer[:500],
        expected_answer=case.expected_answer,
        answer_similarity=round(similarity, 3),
        citation_precision=round(precision, 3),
        citation_recall=round(recall, 3),
        latency_ms=round(latency, 1),
        passed=passed,
    )


async def run_evaluation(
    tenant_id: str,
    dataset_path: str | None = None,
    model: str | None = None,
    tags: list[str] | None = None,
) -> EvalSummary:
    cases = load_golden_dataset(dataset_path)

    if tags:
        cases = [c for c in cases if any(t in c.tags for t in tags)]

    logger.info("Running evaluation: %d cases, prompt version '%s'", len(cases), get_active_version())

    results: list[EvalResult] = []
    for case in cases:
        try:
            result = await evaluate_case(case, tenant_id, model)
            results.append(result)
            status = "PASS" if result.passed else "FAIL"
            logger.info(
                "  [%s] '%s' sim=%.2f prec=%.2f recall=%.2f %.0fms",
                status, case.query[:40], result.answer_similarity,
                result.citation_precision, result.citation_recall, result.latency_ms,
            )
        except Exception as e:
            logger.error("  [ERROR] '%s': %s", case.query[:40], e)

    passed = sum(1 for r in results if r.passed)

    return EvalSummary(
        total_cases=len(results),
        passed=passed,
        failed=len(results) - passed,
        avg_similarity=round(sum(r.answer_similarity for r in results) / max(len(results), 1), 3),
        avg_citation_precision=round(sum(r.citation_precision for r in results) / max(len(results), 1), 3),
        avg_citation_recall=round(sum(r.citation_recall for r in results) / max(len(results), 1), 3),
        avg_latency_ms=round(sum(r.latency_ms for r in results) / max(len(results), 1), 1),
        prompt_version=get_active_version(),
    )
