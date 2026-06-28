# ADR-005: YAML-Based Prompt Versioning

**Status:** Accepted
**Date:** 2026-06-27

## Context

Prompt engineering is iterative. We need to version prompts, A/B test variants, and detect regressions when prompts change.

## Decision

Store prompts in YAML files with version headers. The PromptManager loads templates at startup and supports hot-reload via API. An evaluation framework runs golden dataset tests against each version.

## Structure

```yaml
version: v1
system:
  rag_answer: |
    You are a knowledgeable assistant...
generation:
  answer_prompt: |
    Based on the following sources...
    {context}
    Question: {query}
```

## Evaluation

- 5 golden test cases with expected answers and expected citation indices
- Metrics: semantic similarity (embedding cosine), citation precision, citation recall, latency
- Pass threshold: similarity >= 0.6 AND citation recall >= 0.5

## Rationale

- **YAML over database**: Prompts are version-controlled with code, reviewed in PRs
- **Hot-reload via API**: No restart needed to test new prompts
- **Golden dataset**: Automated regression detection on prompt changes
- **Evaluation results track prompt_version**: Clear attribution of quality changes

## Consequences

- Prompt changes require deployment (or API reload) — intentional, prevents untested changes
- Golden dataset must be maintained as the knowledge base evolves
