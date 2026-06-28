import logging
from pathlib import Path

import yaml

from app.core.config import settings

logger = logging.getLogger(__name__)

_templates: dict = {}
_active_version: str = settings.prompt_version

PROMPTS_DIR = Path(__file__).parent


def load_templates(version: str | None = None) -> dict:
    global _templates, _active_version
    version = version or _active_version

    template_file = PROMPTS_DIR / "templates.yaml"
    versioned_file = PROMPTS_DIR / f"templates_{version}.yaml"

    path = versioned_file if versioned_file.exists() else template_file
    with open(path) as f:
        _templates = yaml.safe_load(f)

    _active_version = _templates.get("version", version)
    logger.info("Loaded prompt templates version '%s' from %s", _active_version, path.name)
    return _templates


def get_template(category: str, name: str) -> str:
    if not _templates:
        load_templates()
    return _templates.get(category, {}).get(name, "")


def get_system_prompt(name: str) -> str:
    return get_template("system", name)


def get_generation_prompt(name: str) -> str:
    return get_template("generation", name)


def build_context(chunks: list[dict], max_chunks: int | None = None) -> str:
    if not _templates:
        load_templates()

    ctx_config = _templates.get("context", {})
    fmt = ctx_config.get("chunk_format", "[Source {index}]: {content}")
    separator = ctx_config.get("separator", "\n\n---\n\n")
    max_chunks = max_chunks or ctx_config.get("max_chunks", 10)

    formatted = []
    for i, chunk in enumerate(chunks[:max_chunks]):
        formatted.append(
            fmt.format(
                index=i + 1,
                title=chunk.get("document_title", "Unknown"),
                content=chunk.get("content", ""),
            )
        )

    return separator.join(formatted)


def build_rag_prompt(query: str, chunks: list[dict], history: list[dict] | None = None) -> tuple[str, str]:
    """Returns (system_prompt, user_prompt)."""
    system = get_system_prompt("rag_answer")
    context = build_context(chunks)

    if history:
        template = get_generation_prompt("answer_with_history")
        history_text = "\n".join(f"{m['role']}: {m['content']}" for m in history[-6:])
        prompt = template.format(context=context, query=query, history=history_text)
    else:
        template = get_generation_prompt("answer_prompt")
        prompt = template.format(context=context, query=query)

    return system, prompt


def get_active_version() -> str:
    return _active_version


def list_versions() -> list[str]:
    versions = []
    for p in PROMPTS_DIR.glob("templates*.yaml"):
        with open(p) as f:
            data = yaml.safe_load(f)
            versions.append(data.get("version", p.stem))
    return versions
