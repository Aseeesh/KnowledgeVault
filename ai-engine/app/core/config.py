from pydantic_settings import BaseSettings


class Settings(BaseSettings):
    ollama_base_url: str = "http://localhost:11434"
    qdrant_host: str = "localhost"
    qdrant_grpc_port: int = 6334
    qdrant_http_port: int = 6333
    redis_url: str = "redis://localhost:6379/1"
    log_level: str = "INFO"

    embedding_model: str = "nomic-embed-text"
    llm_model: str = "llama3.2:1b"
    fallback_embedding_model: str = "nomic-embed-text"
    fallback_llm_model: str = "llama3.2:1b"

    collection_name: str = "document_chunks"
    embedding_dimensions: int = 768
    max_context_tokens: int = 4096
    max_response_tokens: int = 1024
    chunk_batch_size: int = 16

    ollama_timeout: int = 300
    ollama_connect_timeout: int = 10
    ollama_max_retries: int = 3

    embedding_cache_ttl: int = 86400  # 24 hours
    search_cache_ttl: int = 300  # 5 minutes

    prompt_version: str = "v1"

    model_config = {"env_prefix": ""}


settings = Settings()
