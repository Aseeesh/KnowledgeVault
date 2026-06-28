import logging

logger = logging.getLogger(__name__)

try:
    import tiktoken
    _encoder = tiktoken.get_encoding("cl100k_base")
except Exception:
    _encoder = None
    logger.warning("tiktoken unavailable, using word-based approximation")


def count_tokens(text: str) -> int:
    if _encoder:
        return len(_encoder.encode(text))
    return len(text.split()) * 4 // 3


def truncate_to_tokens(text: str, max_tokens: int) -> str:
    if _encoder:
        tokens = _encoder.encode(text)
        if len(tokens) <= max_tokens:
            return text
        return _encoder.decode(tokens[:max_tokens])
    words = text.split()
    approx_words = max_tokens * 3 // 4
    return " ".join(words[:approx_words])


def fits_context(texts: list[str], max_tokens: int) -> tuple[list[str], int]:
    """Return texts that fit within the token budget, plus total tokens used."""
    result = []
    total = 0
    for text in texts:
        tokens = count_tokens(text)
        if total + tokens > max_tokens:
            break
        result.append(text)
        total += tokens
    return result, total
