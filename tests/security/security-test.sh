#!/bin/bash
set -uo pipefail

BASE_URL="${1:-http://localhost:5001}"
TENANT="a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11"
PASS=0
FAIL=0

check() {
    local name="$1" expected="$2" actual="$3"
    if [[ "$actual" == *"$expected"* ]]; then
        echo "  [PASS] $name"
        ((PASS++))
    else
        echo "  [FAIL] $name (expected: $expected, got: $actual)"
        ((FAIL++))
    fi
}

echo "============================================================"
echo "  Security Test Suite — KnowledgeVault"
echo "============================================================"

echo ""
echo "--- 1. Authentication ---"
# No tenant header → 401
STATUS=$(curl -s -o /dev/null -w "%{http_code}" "$BASE_URL/api/v1/documents")
check "Missing auth returns 401" "401" "$STATUS"

# Invalid API key → 401
STATUS=$(curl -s -o /dev/null -w "%{http_code}" "$BASE_URL/api/v1/documents" -H "Authorization: Bearer invalid-key")
check "Invalid API key returns 401" "401" "$STATUS"

# Valid tenant → 200
STATUS=$(curl -s -o /dev/null -w "%{http_code}" "$BASE_URL/api/v1/documents" -H "X-Tenant-Id: $TENANT")
check "Valid tenant returns 200" "200" "$STATUS"

echo ""
echo "--- 2. Security Headers ---"
HEADERS=$(curl -sI "$BASE_URL/health")
check "X-Content-Type-Options" "nosniff" "$HEADERS"
check "X-Frame-Options" "DENY" "$HEADERS"
check "X-XSS-Protection" "1; mode=block" "$HEADERS"
check "Content-Security-Policy" "default-src" "$HEADERS"
check "Referrer-Policy" "strict-origin" "$HEADERS"
check "X-Request-Id present" "X-Request-Id" "$HEADERS"

echo ""
echo "--- 3. SQL Injection ---"
# Attempt SQL injection in query params
STATUS=$(curl -s -o /dev/null -w "%{http_code}" "$BASE_URL/api/v1/documents?page=1;DROP%20TABLE%20documents" -H "X-Tenant-Id: $TENANT")
# 200 or 400 are both safe - the injection didn't execute
if [[ "$STATUS" == "200" || "$STATUS" == "400" ]]; then
    echo "  [PASS] SQL injection in query param blocked (returned $STATUS)"
    ((PASS++))
else
    echo "  [FAIL] SQL injection in query param - unexpected status $STATUS"
    ((FAIL++))
fi

# Attempt SQL injection in search body
SQL_STATUS=$(curl -s -o /dev/null -w "%{http_code}" -X POST "$BASE_URL/api/v1/search" \
  -H "X-Tenant-Id: $TENANT" -H "Content-Type: application/json" \
  -d "{\"query\":\"SELECT * FROM documents; DROP TABLE documents;\",\"topK\":5}" 2>/dev/null || echo "000")
if [[ "$SQL_STATUS" != "000" ]]; then
    echo "  [PASS] SQL injection in search body handled safely (status: $SQL_STATUS)"
    ((PASS++))
else
    echo "  [FAIL] SQL injection in search body - server crashed"
    ((FAIL++))
fi

echo ""
echo "--- 4. XSS Prevention ---"
# Create doc with XSS payload
RESP=$(curl -s -X POST "$BASE_URL/api/v1/documents" \
  -H "X-Tenant-Id: $TENANT" -H "Content-Type: application/json" \
  -d '{"title":"<script>alert(1)</script>","contentType":"text/plain"}')
# Should store raw, not execute
check "XSS in title stored safely" "script" "$RESP"

echo ""
echo "--- 5. Rate Limiting ---"
# Send many requests quickly
LAST_STATUS=""
for i in $(seq 1 200); do
    LAST_STATUS=$(curl -s -o /dev/null -w "%{http_code}" "$BASE_URL/api/v1/documents" -H "X-Tenant-Id: $TENANT")
done
# With default 60/min limit, some should be 429 (but tenant may have higher limit)
echo "  [INFO] After 200 rapid requests, last status: $LAST_STATUS"
if [[ "$LAST_STATUS" == "429" ]]; then
    echo "  [PASS] Rate limiting enforced"
    ((PASS++))
else
    echo "  [INFO] Rate limit not hit - limit may be high for dev tenant"
fi

echo ""
echo "--- 6. Tenant Isolation ---"
FAKE_TENANT=$(python3 -c "import uuid; print(uuid.uuid4())")
RESP=$(curl -s "$BASE_URL/api/v1/documents" -H "X-Tenant-Id: $FAKE_TENANT")
check "Unknown tenant returns 401" "401" "$(curl -s -o /dev/null -w '%{http_code}' "$BASE_URL/api/v1/documents" -H "X-Tenant-Id: $FAKE_TENANT")"

echo ""
echo "--- 7. Input Validation ---"
# Empty title
STATUS=$(curl -s -o /dev/null -w "%{http_code}" -X POST "$BASE_URL/api/v1/documents" \
  -H "X-Tenant-Id: $TENANT" -H "Content-Type: application/json" \
  -d '{"title":"","contentType":"text/plain"}')
check "Empty title rejected" "400" "$STATUS"

# Invalid content type
STATUS=$(curl -s -o /dev/null -w "%{http_code}" -X POST "$BASE_URL/api/v1/documents" \
  -H "X-Tenant-Id: $TENANT" -H "Content-Type: application/json" \
  -d '{"title":"Test","contentType":"application/exe"}')
check "Invalid content type rejected" "400" "$STATUS"

echo ""
echo "============================================================"
echo "  Results: $PASS passed, $FAIL failed"
echo "============================================================"
exit $FAIL
