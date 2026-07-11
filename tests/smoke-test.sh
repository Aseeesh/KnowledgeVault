#!/bin/bash
# smoke-test.sh - Verify all services are working

set -e

echo "🔥 Running smoke tests..."

# Colors
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[0;33m'
NC='\033[0m'

# ─── Check Infrastructure ──────────────────────────────────────
echo ""
echo "📊 Checking infrastructure services..."

check_port() {
    local name=$1
    local port=$2
    
    if nc -z localhost "$port" 2>/dev/null; then
        echo -e "${GREEN}✅ $name is listening on port $port${NC}"
        return 0
    else
        echo -e "${RED}❌ $name is not listening on port $port${NC}"
        return 1
    fi
}

check_port "PostgreSQL" 5432
check_port "Redis" 6379
check_port "Qdrant HTTP" 6333
check_port "Qdrant gRPC" 6334
check_port "RabbitMQ AMQP" 5672
check_port "RabbitMQ UI" 15672

# Check Qdrant health
if curl -sf "http://localhost:6333/healthz" > /dev/null 2>&1; then
    echo -e "${GREEN}✅ Qdrant is healthy${NC}"
else
    echo -e "${RED}❌ Qdrant is not healthy${NC}"
fi

# ─── Check Ollama (Local) ──────────────────────────────────────
echo ""
echo "📦 Checking Ollama (local)..."
if curl -sf "http://localhost:11434/api/tags" > /dev/null 2>&1; then
    echo -e "${GREEN}✅ Ollama is running${NC}"
    
    if curl -sf "http://localhost:11434/api/tags" | grep -q "llama3.2:1b"; then
        echo -e "${GREEN}✅ llama3.2:1b is loaded${NC}"
    else
        echo -e "${YELLOW}⚠️  llama3.2:1b not found (run: ollama pull llama3.2:1b)${NC}"
    fi
    
    if curl -sf "http://localhost:11434/api/tags" | grep -q "nomic-embed-text"; then
        echo -e "${GREEN}✅ nomic-embed-text is loaded${NC}"
    else
        echo -e "${YELLOW}⚠️  nomic-embed-text not found (run: ollama pull nomic-embed-text)${NC}"
    fi
else
    echo -e "${RED}❌ Ollama is not running${NC}"
    echo "   Start Ollama: ollama serve"
fi

# ─── Check Local Services ──────────────────────────────────────
echo ""
echo "📊 Checking local services..."

# Check API
if curl -sf "http://localhost:5001/health" > /dev/null 2>&1; then
    echo -e "${GREEN}✅ API is healthy (http://localhost:5001)${NC}"
else
    echo -e "${YELLOW}⚠️  API not responding (run: make run-api)${NC}"
fi

# Check AI Engine
if curl -sf "http://localhost:8000/health" > /dev/null 2>&1; then
    echo -e "${GREEN}✅ AI Engine is healthy (http://localhost:8000)${NC}"
else
    echo -e "${YELLOW}⚠️  AI Engine not responding (run: make run-ai)${NC}"
fi

# Check Web Client
if curl -sf "http://localhost:5173" > /dev/null 2>&1; then
    echo -e "${GREEN}✅ Web Client is healthy (http://localhost:5173)${NC}"
else
    echo -e "${YELLOW}⚠️  Web Client not responding (run: make run-web)${NC}"
fi

echo ""
echo "🎯 Smoke test complete!"
echo ""
echo "📋 Service URLs:"
echo "   API:        http://localhost:5001/swagger"
echo "   AI Engine:  http://localhost:8000/docs"
echo "   Web Client: http://localhost:5173"
echo "   Qdrant:     http://localhost:6333/dashboard"
echo "   RabbitMQ:   http://localhost:15672 (kv_user / KvRabbit2024!)"
echo ""
echo "🚀 If services aren't running:"
echo "   make run-api  (Terminal 1)"
echo "   make run-ai   (Terminal 2)"
echo "   make run-web  (Terminal 3)"