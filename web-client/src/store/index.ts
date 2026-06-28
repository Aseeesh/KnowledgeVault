import { create } from 'zustand';
import type { ChatMessage, ChatSession, Theme } from '../types';

interface AppState {
  theme: Theme;
  tenantId: string;
  toggleTheme: () => void;

  // Chat
  sessions: ChatSession[];
  activeSessionId: string | null;
  streamingContent: string;
  isStreaming: boolean;
  setActiveSession: (id: string | null) => void;
  addSession: (session: ChatSession) => void;
  addMessage: (sessionId: string, message: ChatMessage) => void;
  updateStreamingContent: (content: string) => void;
  setIsStreaming: (v: boolean) => void;
  setMessageFeedback: (sessionId: string, messageId: string, rating: number, comment?: string) => void;
}

export const useAppStore = create<AppState>((set) => ({
  theme: 'dark',
  tenantId: 'a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11',
  toggleTheme: () => set((s) => ({ theme: s.theme === 'dark' ? 'light' : 'dark' })),

  sessions: [],
  activeSessionId: null,
  streamingContent: '',
  isStreaming: false,
  setActiveSession: (id) => set({ activeSessionId: id }),
  addSession: (session) => set((s) => ({ sessions: [session, ...s.sessions], activeSessionId: session.id })),
  addMessage: (sessionId, message) =>
    set((s) => ({
      sessions: s.sessions.map((sess) =>
        sess.id === sessionId ? { ...sess, messages: [...sess.messages, message] } : sess
      ),
    })),
  updateStreamingContent: (content) => set({ streamingContent: content }),
  setIsStreaming: (v) => set({ isStreaming: v }),
  setMessageFeedback: (sessionId, messageId, rating, comment) =>
    set((s) => ({
      sessions: s.sessions.map((sess) =>
        sess.id === sessionId
          ? {
              ...sess,
              messages: sess.messages.map((m) =>
                m.id === messageId ? { ...m, feedback: { rating, comment } } : m
              ),
            }
          : sess
      ),
    })),
}));
