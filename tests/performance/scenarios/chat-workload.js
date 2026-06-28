import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend, Counter } from 'k6/metrics';

const chatLatency = new Trend('chat_latency_ms', true);
const searchLatency = new Trend('search_latency_ms', true);
const ttft = new Trend('time_to_first_token_ms', true);
const errorRate = new Rate('error_rate');
const chatRequests = new Counter('chat_requests');
const searchRequests = new Counter('search_requests');

const BASE = __ENV.BASE_URL || 'http://localhost:5001';
const AI_BASE = __ENV.AI_URL || 'http://localhost:8000';
const TENANT = 'a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11';
const HEADERS = { 'Content-Type': 'application/json', 'X-Tenant-Id': TENANT };

const QUERIES = [
  'What is Retrieval-Augmented Generation?',
  'How does BM25 scoring work?',
  'Explain vector databases',
  'What is Reciprocal Rank Fusion?',
  'How does citation verification work?',
  'What are embedding models?',
  'Explain hybrid search',
  'What is semantic similarity?',
  'How do you chunk documents?',
  'What is cosine similarity?',
];

export const options = {
  scenarios: {
    // Scenario 1: Light chat usage
    light_chat: {
      executor: 'constant-arrival-rate',
      rate: 2,
      timeUnit: '1s',
      duration: '1m',
      preAllocatedVUs: 5,
      maxVUs: 10,
      exec: 'chatWorkload',
      startTime: '0s',
    },
    // Scenario 2: Search-heavy
    search_heavy: {
      executor: 'constant-arrival-rate',
      rate: 10,
      timeUnit: '1s',
      duration: '1m',
      preAllocatedVUs: 15,
      maxVUs: 30,
      exec: 'searchWorkload',
      startTime: '0s',
    },
    // Scenario 3: Spike test
    spike: {
      executor: 'ramping-arrival-rate',
      startRate: 1,
      timeUnit: '1s',
      stages: [
        { target: 5, duration: '30s' },
        { target: 20, duration: '10s' },  // spike
        { target: 5, duration: '30s' },
        { target: 0, duration: '10s' },
      ],
      preAllocatedVUs: 30,
      maxVUs: 50,
      exec: 'mixedWorkload',
      startTime: '1m',
    },
  },
  thresholds: {
    'chat_latency_ms': ['p(95)<10000', 'p(50)<5000'],
    'search_latency_ms': ['p(95)<2000', 'p(50)<500'],
    'error_rate': ['rate<0.05'],
    'http_req_duration': ['p(99)<15000'],
  },
};

export function chatWorkload() {
  const query = QUERIES[Math.floor(Math.random() * QUERIES.length)];
  const start = Date.now();

  const res = http.post(`${AI_BASE}/generate/rag`, JSON.stringify({
    tenant_id: TENANT, query, top_k: 3,
  }), { headers: HEADERS, timeout: '30s' });

  const latency = Date.now() - start;
  chatLatency.add(latency);
  chatRequests.add(1);
  errorRate.add(res.status !== 200);

  check(res, {
    'chat 200': (r) => r.status === 200,
    'has answer': (r) => r.json('answer') !== undefined,
    'has citations': (r) => r.json('citations') !== undefined,
  });

  sleep(Math.random() * 2);
}

export function searchWorkload() {
  const query = QUERIES[Math.floor(Math.random() * QUERIES.length)];
  const start = Date.now();

  const res = http.post(`${BASE}/api/v1/search`, JSON.stringify({
    query, topK: 5,
  }), { headers: HEADERS, timeout: '10s' });

  const latency = Date.now() - start;
  searchLatency.add(latency);
  searchRequests.add(1);
  errorRate.add(res.status !== 200);

  check(res, {
    'search 200': (r) => r.status === 200,
    'has results': (r) => r.json('totalResults') >= 0,
  });
}

export function mixedWorkload() {
  if (Math.random() < 0.3) {
    chatWorkload();
  } else {
    searchWorkload();
  }
}

export function handleSummary(data) {
  return {
    stdout: generateReport(data),
    'reports/benchmark-results.json': JSON.stringify(data, null, 2),
  };
}

function generateReport(data) {
  const m = data.metrics;
  const fmt = (v) => v ? v.toFixed(1) : 'N/A';

  return `
╔══════════════════════════════════════════════════════════════╗
║          KnowledgeVault Performance Benchmark Report         ║
╠══════════════════════════════════════════════════════════════╣
║                                                              ║
║  LATENCY                                                     ║
║  ────────────────────────────────────────                    ║
║  Chat P50:    ${fmt(m.chat_latency_ms?.values?.med)}ms                                    ║
║  Chat P95:    ${fmt(m.chat_latency_ms?.values?.['p(95)'])}ms                                    ║
║  Chat P99:    ${fmt(m.chat_latency_ms?.values?.['p(99)'])}ms                                    ║
║  Search P50:  ${fmt(m.search_latency_ms?.values?.med)}ms                                     ║
║  Search P95:  ${fmt(m.search_latency_ms?.values?.['p(95)'])}ms                                     ║
║                                                              ║
║  THROUGHPUT                                                  ║
║  ────────────────────────────────────────                    ║
║  Total Reqs:  ${m.http_reqs?.values?.count || 0}                                           ║
║  Req/s:       ${fmt(m.http_reqs?.values?.rate)}                                         ║
║  Chat Reqs:   ${m.chat_requests?.values?.count || 0}                                           ║
║  Search Reqs: ${m.search_requests?.values?.count || 0}                                           ║
║                                                              ║
║  RELIABILITY                                                 ║
║  ────────────────────────────────────────                    ║
║  Error Rate:  ${((m.error_rate?.values?.rate || 0) * 100).toFixed(2)}%                                        ║
║  HTTP Errors: ${m.http_req_failed?.values?.passes || 0}                                            ║
║                                                              ║
╚══════════════════════════════════════════════════════════════╝
`;
}
