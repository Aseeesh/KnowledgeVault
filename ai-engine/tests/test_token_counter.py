from app.services.token_counter import count_tokens, truncate_to_tokens, fits_context


def test_count_tokens_non_empty():
    assert count_tokens("Hello world") > 0


def test_count_tokens_empty():
    assert count_tokens("") == 0


def test_truncate_short_text():
    text = "This is short"
    assert truncate_to_tokens(text, 1000) == text


def test_truncate_long_text():
    text = " ".join(f"word{i}" for i in range(1000))
    result = truncate_to_tokens(text, 50)
    assert count_tokens(result) <= 60  # Allow approximation margin


def test_fits_context_all_fit():
    texts = ["Short text one.", "Short text two.", "Short text three."]
    result, total = fits_context(texts, 1000)
    assert len(result) == 3
    assert total > 0


def test_fits_context_budget_exceeded():
    texts = [" ".join(f"word{i}" for i in range(500)) for _ in range(10)]
    result, total = fits_context(texts, 100)
    assert len(result) < 10
    assert total <= 120  # Allow approximation margin
