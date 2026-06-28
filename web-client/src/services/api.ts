import axios from 'axios';
import type {
  Document, PaginatedResponse, SearchResponse,
  ChatCompletionResponse, IngestionJob, DashboardStats,
} from '../types';

const API = axios.create({ baseURL: 'http://localhost:5001', headers: { 'Content-Type': 'application/json' } });

const tenant = () => ({ 'X-Tenant-Id': 'a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11' });

// Documents
export const listDocuments = (page = 1, pageSize = 20) =>
  API.get<PaginatedResponse<Document>>('/api/v1/documents', { params: { page, pageSize }, headers: tenant() });

export const getDocument = (id: string) =>
  API.get<Document>(`/api/v1/documents/${id}`, { headers: tenant() });

export const createDocument = (data: { title: string; contentType: string; sourceUrl?: string }) =>
  API.post<Document>('/api/v1/documents', data, { headers: tenant() });

export const deleteDocument = (id: string) =>
  API.delete(`/api/v1/documents/${id}`, { headers: tenant() });

export const uploadDocument = (title: string, file: File) => {
  const form = new FormData();
  form.append('title', title);
  form.append('file', file);
  return API.post<Document>('/api/v1/documents/upload', form, {
    headers: { ...tenant(), 'Content-Type': 'multipart/form-data' },
  });
};

// Search
export const hybridSearch = (query: string, topK = 10) =>
  API.post<SearchResponse>('/api/v1/search', { query, topK }, { headers: tenant() });

// Chat
export const chatCompletion = (message: string, sessionId?: string) =>
  API.post<ChatCompletionResponse>('/api/v1/chat/completions', { message, sessionId }, { headers: tenant() });

export const submitFeedback = (messageId: string, sessionId: string, rating: number, comment?: string) =>
  API.post(`/api/v1/chat/messages/${messageId}/feedback`, { sessionId, rating, comment }, { headers: tenant() });

// Ingestion
export const getIngestionStatus = (jobId: string) =>
  API.get<IngestionJob>(`/api/v1/ingestion/jobs/${jobId}`, { headers: tenant() });

// Health
export const checkHealth = () => API.get('/health');
export const checkAIHealth = () => axios.get('http://localhost:8000/health');

// Dashboard stats (computed client-side from available endpoints)
export const getDashboardStats = async (): Promise<DashboardStats> => {
  const docs = await listDocuments(1, 1);
  return {
    totalDocuments: docs.data.totalCount,
    totalChunks: 0,
    totalSessions: 0,
    avgConfidence: 0,
    hallucinationRate: 0,
    avgResponseTimeMs: 0,
  };
};
