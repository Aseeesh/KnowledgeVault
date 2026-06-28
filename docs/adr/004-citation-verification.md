# ADR-004: Citation Verification with Confidence Scoring

**Status:** Accepted
**Date:** 2026-06-27

## Context

LLMs can hallucinate — generating plausible but unsupported claims. We need a post-generation verification step to assess whether the answer is grounded in the retrieved sources.

## Decision

Implement a two-stage citation verification pipeline:

1. **Source reference extraction**: Parse `[Source N]` patterns from the LLM response
2. **Claim-level verification**: Extract factual claims from the response, embed each claim and each source chunk, compute cosine similarity to find supporting evidence

## Confidence Score Formula

```
confidence = 0.4 × avg_citation_confidence
           + 0.3 × citation_coverage (citations / chunks)
           + 0.2 × answer_length_score (min(words/50, 1))
           - 0.2 × hedging_penalty (if "I cannot", "insufficient", etc.)
```

## Hallucination Detection

Flag `hasPotentialHallucinations = true` when > 30% of extracted claims have no supporting source chunk (similarity < 0.5).

## Rationale

- **Embedding-based verification** is model-agnostic and doesn't require an additional LLM call
- **Cosine similarity threshold** (0.5 for match, 0.7 for "verified") balances precision/recall
- **Hedging detection** catches when the model admits uncertainty — reduces confidence appropriately
- **Lightweight**: adds ~100ms to the pipeline (embedding comparison, no extra LLM call)

## Consequences

- Requires embedding both claims and chunks — mitigated by Redis caching
- Similarity threshold may need tuning per domain
- Does not catch subtle factual errors within a correctly-cited source
