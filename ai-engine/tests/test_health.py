from unittest.mock import AsyncMock, patch

import pytest
from fastapi.testclient import TestClient


@pytest.fixture
def client():
    with patch("app.services.vector_store.vector_store.ensure_collection", new_callable=AsyncMock):
        from app.main import app
        return TestClient(app)


def test_health_endpoint(client):
    response = client.get("/health")
    assert response.status_code == 200
    data = response.json()
    assert data["service"] == "ai-engine"
    assert "status" in data
    assert "ollama_connected" in data
    assert "qdrant_connected" in data
    assert "redis_connected" in data
