from prometheus_client import Counter, Histogram, Gauge


class Metrics:
    def __init__(self):
        self.embedding_cache_hits = Counter("kv_embedding_cache_hits_total", "Embedding cache hits")
        self.embedding_cache_misses = Counter("kv_embedding_cache_misses_total", "Embedding cache misses")
        self.embeddings_generated = Counter("kv_embeddings_generated_total", "Embeddings generated")
        self.embedding_latency = Histogram("kv_embedding_latency_seconds", "Embedding generation latency")

        self.search_requests = Counter("kv_search_requests_total", "Search requests")
        self.search_latency = Histogram("kv_search_latency_seconds", "Search latency")

        self.generation_latency = Histogram("kv_generation_latency_seconds", "LLM generation latency")
        self.rag_requests = Counter("kv_rag_requests_total", "RAG pipeline requests")

        self.active_models = Gauge("kv_active_models", "Number of loaded Ollama models")


metrics = Metrics()
