import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend } from 'k6/metrics';

const errorRate = new Rate('errors');
const searchLatency = new Trend('search_latency');
const chatLatency = new Trend('chat_latency');

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5001';
const TENANT_ID = 'a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11';

const HEADERS = {
  'Content-Type': 'application/json',
  'X-Tenant-Id': TENANT_ID,
};

export const options = {
  scenarios: {
    // Smoke test: verify system works under minimal load
    smoke: {
      executor: 'constant-vus',
      vus: 1,
      duration: '30s',
      exec: 'smokeTest',
      tags: { scenario: 'smoke' },
    },
    // Load test: normal expected load
    load: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '1m', target: 10 },
        { duration: '3m', target: 10 },
        { duration: '1m', target: 0 },
      ],
      exec: 'loadTest',
      tags: { scenario: 'load' },
      startTime: '30s',
    },
    // Stress test: beyond normal capacity
    stress: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '1m', target: 50 },
        { duration: '2m', target: 50 },
        { duration: '1m', target: 100 },
        { duration: '2m', target: 100 },
        { duration: '1m', target: 0 },
      ],
      exec: 'loadTest',
      tags: { scenario: 'stress' },
      startTime: '6m',
    },
  },
  thresholds: {
    http_req_duration: ['p(95)<5000'],
    errors: ['rate<0.1'],
    search_latency: ['p(95)<3000'],
  },
};

const QUERIES = [
  'What is RAG?',
  'How does vector search work?',
  'Explain BM25 algorithm',
  'What is citation verification?',
  'How does Reciprocal Rank Fusion work?',
];

export function smokeTest() {
  const healthRes = http.get(`${BASE_URL}/health`);
  check(healthRes, { 'health 200': (r) => r.status === 200 });

  const docsRes = http.get(`${BASE_URL}/api/v1/documents`, { headers: HEADERS });
  check(docsRes, { 'docs 200': (r) => r.status === 200 });

  sleep(1);
}

export function loadTest() {
  const query = QUERIES[Math.floor(Math.random() * QUERIES.length)];

  // Search endpoint
  const searchRes = http.post(
    `${BASE_URL}/api/v1/search`,
    JSON.stringify({ query, topK: 5 }),
    { headers: HEADERS }
  );
  check(searchRes, { 'search 200': (r) => r.status === 200 });
  errorRate.add(searchRes.status !== 200);
  searchLatency.add(searchRes.timings.duration);

  // Document list
  const docsRes = http.get(`${BASE_URL}/api/v1/documents?page=1&pageSize=10`, { headers: HEADERS });
  check(docsRes, { 'docs 200': (r) => r.status === 200 });
  errorRate.add(docsRes.status !== 200);

  sleep(Math.random() * 2 + 1);
}

export function handleSummary(data) {
  return {
    'stdout': textSummary(data, { indent: '  ', enableColors: true }),
    'results.json': JSON.stringify(data, null, 2),
  };
}

function textSummary(data) {
  const metrics = data.metrics;
  return `
=== KnowledgeVault Load Test Results ===
Requests:     ${metrics.http_reqs?.values?.count || 0}
Duration:     ${(metrics.http_req_duration?.values?.avg || 0).toFixed(0)}ms avg, ${(metrics.http_req_duration?.values?.['p(95)'] || 0).toFixed(0)}ms p95
Error rate:   ${((metrics.errors?.values?.rate || 0) * 100).toFixed(1)}%
Search P95:   ${(metrics.search_latency?.values?.['p(95)'] || 0).toFixed(0)}ms
`;
}
