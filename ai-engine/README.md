# KnowledgeVault AI Engine

Python-based AI service powering the search and generation capabilities of KnowledgeVault. Implements hybrid retrieval (dense vectors + BM25) with Reciprocal Rank Fusion and LLM-powered answer generation.

## Tech Stack

- **Framework**: FastAPI
- **Vector DB**: Qdrant (gRPC client)
- **LLM Runtime**: Ollama (llama3.2:1b, nomic-embed-text)
- **Sparse Search**: rank-bm25

## How Hybrid Search Works

1. **Query embedding** — Convert query to dense vector via Ollama (nomic-embed-text)
2. **Dense retrieval** — Search Qdrant for similar vectors, filtered by tenant
3. **BM25 reranking** — Apply sparse keyword scoring on the dense result set
4. **Reciprocal Rank Fusion** — Merge dense + sparse rankings into a unified score

## Running Locally

```bash
# Requires Ollama running with models pulled
pip install -r requirements.txt
uvicorn app.main:app --reload --port 8000
```

API docs at `http://localhost:8000/docs`

## Endpoints

| Method | Path | Description |
|--------|------|-------------|
| GET | `/health` | Service health |
| POST | `/embeddings/` | Generate text embeddings |
| POST | `/search/hybrid` | Hybrid search with RRF |
| POST | `/generate/` | RAG answer with citations |



# Navigate to AI Engine directory
cd ai-engine

# Create virtual environment
python3 -m venv venv

# Activate virtual environment
# Make sure you're in the ai-engine directory with venv activated
cd  ai-engine
source venv/bin/activate  # if not already activated

# Run the server
python -m uvicorn app.main:app --reload --host 0.0.0.0 --port 8000

pip install --upgrade pip
pip install -r requirements.txt
