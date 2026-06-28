export interface Tenant {
  id: string;
  name: string;
  slug: string;
}

export interface Document {
  id: string;
  title: string;
  contentType: string;
  status: 'Pending' | 'Processing' | 'Indexed' | 'Failed' | 'Stale';
  version: number;
  chunkCount: number;
  sourceUrl?: string;
  createdAt: string;
  indexedAt?: string;
}

export interface PaginatedResponse<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface SearchHit {
  documentId: string;
  chunkId: string;
  content: string;
  documentTitle: string;
  sectionHeading?: string;
  denseScore: number;
  sparseScore: number;
  fusedScore: number;
  rerankedScore: number;
}

export interface SearchResponse {
  query: string;
  hits: SearchHit[];
  totalResults: number;
  responseTimeMs: number;
}

export interface Citation {
  chunkId: string;
  documentTitle: string;
  excerpt: string;
  confidence: number;
  verified: boolean;
}

export interface ChatMessage {
  id: string;
  role: 'user' | 'assistant';
  content: string;
  citations?: Citation[];
  confidenceScore?: number;
  createdAt: string;
  isStreaming?: boolean;
  feedback?: { rating: number; comment?: string };
}

export interface ChatSession {
  id: string;
  title?: string;
  messages: ChatMessage[];
  createdAt: string;
}

export interface ChatCompletionResponse {
  sessionId: string;
  messageId: string;
  answer: string;
  citations: Citation[];
  confidenceScore: number;
  hasPotentialHallucinations: boolean;
}

export interface IngestionJob {
  jobId: string;
  documentId: string;
  status: string;
  errorMessage?: string;
  retryCount: number;
  createdAt: string;
  completedAt?: string;
}

export interface DashboardStats {
  totalDocuments: number;
  totalChunks: number;
  totalSessions: number;
  avgConfidence: number;
  hallucinationRate: number;
  avgResponseTimeMs: number;
}

export type Theme = 'dark' | 'light';
