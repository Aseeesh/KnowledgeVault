from app.prompts.manager import load_templates, get_template, build_context, build_rag_prompt, get_active_version, list_versions


def test_load_templates():
    templates = load_templates()
    assert "version" in templates
    assert "system" in templates
    assert "generation" in templates


def test_get_system_prompt():
    load_templates()
    template = get_template("system", "rag_answer")
    assert "citation" in template.lower() or "source" in template.lower()
    assert len(template) > 50


def test_get_generation_prompt():
    load_templates()
    template = get_template("generation", "answer_prompt")
    assert "{context}" in template
    assert "{query}" in template


def test_build_context():
    load_templates()
    chunks = [
        {"content": "First chunk content", "document_title": "Doc A"},
        {"content": "Second chunk content", "document_title": "Doc B"},
    ]
    context = build_context(chunks)
    assert "Source 1" in context
    assert "Source 2" in context
    assert "First chunk content" in context


def test_build_context_respects_max():
    load_templates()
    chunks = [{"content": f"Chunk {i}", "document_title": f"Doc {i}"} for i in range(20)]
    context = build_context(chunks, max_chunks=3)
    assert "Source 4" not in context


def test_build_rag_prompt():
    load_templates()
    chunks = [{"content": "RAG combines retrieval.", "document_title": "Doc"}]
    system, prompt = build_rag_prompt("What is RAG?", chunks)
    assert len(system) > 0
    assert "What is RAG?" in prompt
    assert "RAG combines retrieval" in prompt


def test_build_rag_prompt_with_history():
    load_templates()
    chunks = [{"content": "Content here.", "document_title": "Doc"}]
    history = [{"role": "user", "content": "Previous question"}, {"role": "assistant", "content": "Previous answer"}]
    system, prompt = build_rag_prompt("Follow up?", chunks, history)
    assert "Previous question" in prompt


def test_version_management():
    load_templates()
    version = get_active_version()
    assert version == "v1"
    versions = list_versions()
    assert "v1" in versions
