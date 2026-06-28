from fastapi import APIRouter

from app.prompts.manager import (
    get_active_version, list_versions, load_templates, get_template,
)

router = APIRouter()


@router.get("/version")
async def active_version():
    return {"version": get_active_version(), "available": list_versions()}


@router.post("/reload")
async def reload_prompts(version: str | None = None):
    templates = load_templates(version)
    return {"status": "reloaded", "version": templates.get("version", "unknown")}


@router.get("/template/{category}/{name}")
async def get_prompt_template(category: str, name: str):
    text = get_template(category, name)
    if not text:
        return {"error": f"Template '{category}/{name}' not found"}
    return {"category": category, "name": name, "template": text}
