from app.services.rag_pipeline import extract_citations, compute_confidence
from app.models.schemas import Citation


def test_extract_citations_with_source_refs():
    answer = "RAG works by [Source 1] retrieving and [Source 2] generating."
    chunks = [
        {"content": "Retrieval-Augmented Generation combines retrieval with generation.", "document_title": "Doc 1"},
        {"content": "BM25 is a sparse ranking function.", "document_title": "Doc 2"},
    ]

    citations = extract_citations(answer, chunks)

    assert len(citations) == 2
    assert citations[0].chunk_index == 0
    assert citations[1].chunk_index == 1


def test_extract_citations_no_refs_uses_overlap():
    answer = "Retrieval augmented generation combines retrieval with text generation for factual answers."
    chunks = [
        {"content": "Retrieval augmented generation combines retrieval with generation.", "document_title": "Doc 1"},
    ]

    citations = extract_citations(answer, chunks)

    assert len(citations) >= 1


def test_extract_citations_empty_chunks():
    citations = extract_citations("Some answer", [])
    assert len(citations) == 0


def test_extract_citations_out_of_range_ignored():
    answer = "Reference [Source 99] is invalid."
    chunks = [{"content": "Only one chunk.", "document_title": "Doc 1"}]

    citations = extract_citations(answer, chunks)
    assert all(c.chunk_index < len(chunks) for c in citations)


def test_compute_confidence_with_citations():
    citations = [Citation(chunk_index=0, text="source", confidence=0.9)]
    chunks = [{"content": "some content"}]
    answer = " ".join(f"word{i}" for i in range(60))

    score = compute_confidence(citations, chunks, answer)

    assert 0 <= score <= 1
    assert score > 0.3


def test_compute_confidence_no_chunks():
    score = compute_confidence([], [], "answer")
    assert score == 0.0


def test_compute_confidence_hedging_reduces_score():
    citations = [Citation(chunk_index=0, text="source", confidence=0.8)]
    chunks = [{"content": "content"}]

    normal = compute_confidence(citations, chunks, "This is a clear factual answer with enough words to pass the threshold here.")
    hedged = compute_confidence(citations, chunks, "I cannot fully answer based on the available sources but here is what I know about the topic.")

    assert hedged < normal


def test_compute_confidence_more_citations_higher_score():
    one = [Citation(chunk_index=0, text="s", confidence=0.8)]
    many = [Citation(chunk_index=i, text="s", confidence=0.8) for i in range(5)]
    chunks = [{"content": f"chunk {i}"} for i in range(5)]
    answer = " ".join(f"word{i}" for i in range(60))

    score_one = compute_confidence(one, chunks, answer)
    score_many = compute_confidence(many, chunks, answer)

    assert score_many > score_one
