#!/bin/bash
# KnowledgeVault Performance Benchmark Suite
# Runs against live local stack — no external tools required

API="http://localhost:5001"
AI="http://localhost:8000"
TENANT="a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11"
HDRS='-H Content-Type:application/json -H X-Tenant-Id:'$TENANT

QUERIES=(
  "What is Retrieval-Augmented Generation?"
  "How does BM25 scoring work?"
  "Explain vector databases"
  "What is Reciprocal Rank Fusion?"
  "How does citation verification work?"
)

declare -a SEARCH_TIMES EMBED_TIMES CHAT_TIMES RAG_TIMES DOC_TIMES
ERRORS=0
TOTAL=0

bench() {
    local label="$1" url="$2" method="$3" body="$4"
    local start end elapsed status

    start=$(python3 -c "import time; print(int(time.time()*1000))")

    if [[ "$method" == "GET" ]]; then
        status=$(curl -s -o /dev/null -w "%{http_code}" "$url" -H "X-Tenant-Id: $TENANT")
    else
        status=$(curl -s -o /dev/null -w "%{http_code}" -X POST "$url" \
            -H "Content-Type: application/json" -H "X-Tenant-Id: $TENANT" \
            -d "$body")
    fi

    end=$(python3 -c "import time; print(int(time.time()*1000))")
    elapsed=$((end - start))
    ((TOTAL++))

    if [[ "$status" != "200" && "$status" != "201" && "$status" != "202" ]]; then
        ((ERRORS++))
    fi

    echo "$elapsed"
}

percentile() {
    local -n arr=$1
    local p=$2
    local sorted=($(printf '%s\n' "${arr[@]}" | sort -n))
    local n=${#sorted[@]}
    if [[ $n -eq 0 ]]; then echo "0"; return; fi
    local idx=$(( (p * n / 100) ))
    if [[ $idx -ge $n ]]; then idx=$((n-1)); fi
    echo "${sorted[$idx]}"
}

average() {
    local -n arr=$1
    local sum=0 n=${#arr[@]}
    if [[ $n -eq 0 ]]; then echo "0"; return; fi
    for v in "${arr[@]}"; do sum=$((sum + v)); done
    echo $((sum / n))
}

echo "╔══════════════════════════════════════════════════════════════╗"
echo "║       KnowledgeVault Performance Benchmark Suite            ║"
echo "╠══════════════════════════════════════════════════════════════╣"
echo ""

# --- Health Check ---
echo "--- Verifying services ---"
API_OK=$(curl -s -o /dev/null -w "%{http_code}" "$API/health")
AI_OK=$(curl -s -o /dev/null -w "%{http_code}" "$AI/health")
echo "  API: $API_OK  AI Engine: $AI_OK"
if [[ "$API_OK" != "200" || "$AI_OK" != "200" ]]; then
    echo "  ERROR: Services not healthy. Aborting."
    exit 1
fi
echo ""

# --- Benchmark 1: Document List ---
echo "--- Benchmark 1: Document List (10 iterations) ---"
for i in $(seq 1 10); do
    t=$(bench "doc-list" "$API/api/v1/documents" "GET" "")
    DOC_TIMES+=("$t")
done
echo "  P50: $(percentile DOC_TIMES 50)ms  P95: $(percentile DOC_TIMES 95)ms  Avg: $(average DOC_TIMES)ms"

# --- Benchmark 2: Embedding Generation ---
echo "--- Benchmark 2: Embedding (5 iterations) ---"
for i in $(seq 1 5); do
    q="${QUERIES[$((i % ${#QUERIES[@]}))]}"
    t=$(bench "embed" "$AI/embeddings/" "POST" "{\"texts\":[\"$q\"]}")
    EMBED_TIMES+=("$t")
done
echo "  P50: $(percentile EMBED_TIMES 50)ms  P95: $(percentile EMBED_TIMES 95)ms  Avg: $(average EMBED_TIMES)ms"

# --- Benchmark 3: Hybrid Search ---
echo "--- Benchmark 3: Hybrid Search (10 iterations) ---"
for i in $(seq 1 10); do
    q="${QUERIES[$((i % ${#QUERIES[@]}))]}"
    t=$(bench "search" "$API/api/v1/search" "POST" "{\"query\":\"$q\",\"topK\":5}")
    SEARCH_TIMES+=("$t")
done
echo "  P50: $(percentile SEARCH_TIMES 50)ms  P95: $(percentile SEARCH_TIMES 95)ms  Avg: $(average SEARCH_TIMES)ms"

# --- Benchmark 4: RAG Pipeline ---
echo "--- Benchmark 4: RAG Pipeline (3 iterations) ---"
for i in $(seq 1 3); do
    q="${QUERIES[$((i % ${#QUERIES[@]}))]}"
    t=$(bench "rag" "$AI/generate/rag" "POST" "{\"tenant_id\":\"$TENANT\",\"query\":\"$q\",\"top_k\":3}")
    RAG_TIMES+=("$t")
done
echo "  P50: $(percentile RAG_TIMES 50)ms  P95: $(percentile RAG_TIMES 95)ms  Avg: $(average RAG_TIMES)ms"

# --- Benchmark 5: Chat Completion ---
echo "--- Benchmark 5: Chat Completion (3 iterations) ---"
for i in $(seq 1 3); do
    q="${QUERIES[$((i % ${#QUERIES[@]}))]}"
    t=$(bench "chat" "$API/api/v1/chat/completions" "POST" "{\"message\":\"$q\"}")
    CHAT_TIMES+=("$t")
done
echo "  P50: $(percentile CHAT_TIMES 50)ms  P95: $(percentile CHAT_TIMES 95)ms  Avg: $(average CHAT_TIMES)ms"

# --- Resource Usage ---
echo ""
echo "--- Resource Usage ---"
docker stats --no-stream --format "table {{.Name}}\t{{.CPUPerc}}\t{{.MemUsage}}\t{{.NetIO}}" 2>/dev/null | head -10

# --- Prometheus Metrics ---
echo ""
echo "--- AI Engine Metrics ---"
curl -s "$AI/metrics" 2>/dev/null | grep -E "^kv_.*total |^kv_.*_count " | head -10

# --- Summary ---
echo ""
echo "╔══════════════════════════════════════════════════════════════╗"
echo "║                    BENCHMARK SUMMARY                        ║"
echo "╠══════════════════════════════════════════════════════════════╣"
printf "║  %-16s  %8s  %8s  %8s  %8s  ║\n" "Endpoint" "P50" "P95" "Avg" "Count"
echo "║  ────────────────  ────────  ────────  ────────  ────────  ║"
printf "║  %-16s  %6sms  %6sms  %6sms  %8s  ║\n" "Document List" "$(percentile DOC_TIMES 50)" "$(percentile DOC_TIMES 95)" "$(average DOC_TIMES)" "${#DOC_TIMES[@]}"
printf "║  %-16s  %6sms  %6sms  %6sms  %8s  ║\n" "Embeddings" "$(percentile EMBED_TIMES 50)" "$(percentile EMBED_TIMES 95)" "$(average EMBED_TIMES)" "${#EMBED_TIMES[@]}"
printf "║  %-16s  %6sms  %6sms  %6sms  %8s  ║\n" "Hybrid Search" "$(percentile SEARCH_TIMES 50)" "$(percentile SEARCH_TIMES 95)" "$(average SEARCH_TIMES)" "${#SEARCH_TIMES[@]}"
printf "║  %-16s  %6sms  %6sms  %6sms  %8s  ║\n" "RAG Pipeline" "$(percentile RAG_TIMES 50)" "$(percentile RAG_TIMES 95)" "$(average RAG_TIMES)" "${#RAG_TIMES[@]}"
printf "║  %-16s  %6sms  %6sms  %6sms  %8s  ║\n" "Chat Completion" "$(percentile CHAT_TIMES 50)" "$(percentile CHAT_TIMES 95)" "$(average CHAT_TIMES)" "${#CHAT_TIMES[@]}"
echo "║                                                              ║"
printf "║  Total requests: %-4s  Errors: %-4s  Error rate: %5.1f%%     ║\n" "$TOTAL" "$ERRORS" "$(python3 -c "print(${ERRORS}/${TOTAL}*100 if ${TOTAL}>0 else 0)")"
echo "╚══════════════════════════════════════════════════════════════╝"
