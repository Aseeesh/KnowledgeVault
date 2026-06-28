import { useState, useCallback, useRef } from 'react';
import { useAppStore } from '../store';
import { chatCompletion, submitFeedback } from '../services/api';
import type { ChatMessage, Citation } from '../types';

export function useChat() {
  const {
    sessions, activeSessionId, isStreaming,
    addSession, addMessage, setActiveSession,
    setIsStreaming, updateStreamingContent, setMessageFeedback,
  } = useAppStore();
  const [error, setError] = useState<string | null>(null);
  const abortRef = useRef<AbortController | null>(null);

  const activeSession = sessions.find((s) => s.id === activeSessionId);

  const sendMessage = useCallback(async (content: string, useStreaming = false) => {
    setError(null);
    const userMsg: ChatMessage = {
      id: crypto.randomUUID(),
      role: 'user',
      content,
      createdAt: new Date().toISOString(),
    };

    if (activeSessionId) {
      addMessage(activeSessionId, userMsg);
    }

    if (useStreaming) {
      setIsStreaming(true);
      updateStreamingContent('');
      abortRef.current = new AbortController();

      try {
        const resp = await fetch('http://localhost:5001/api/v1/chat/completions/stream', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json', 'X-Tenant-Id': 'a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11' },
          body: JSON.stringify({ message: content, sessionId: activeSessionId }),
          signal: abortRef.current.signal,
        });

        const reader = resp.body?.getReader();
        const decoder = new TextDecoder();
        let accumulated = '';
        let citations: Citation[] = [];
        let confidence = 0;

        while (reader) {
          const { done, value } = await reader.read();
          if (done) break;

          const text = decoder.decode(value);
          for (const line of text.split('\n')) {
            if (!line.startsWith('data: ')) continue;
            const data = line.slice(6).trim();

            if (data === '[DONE]') break;
            if (data.startsWith('[CITATIONS]')) {
              try { citations = JSON.parse(data.slice(11)); } catch { /* ignore */ }
              continue;
            }
            if (data.startsWith('[CONFIDENCE]')) {
              confidence = parseFloat(data.slice(12));
              continue;
            }

            accumulated += data;
            updateStreamingContent(accumulated);
          }
        }

        const assistantMsg: ChatMessage = {
          id: crypto.randomUUID(),
          role: 'assistant',
          content: accumulated,
          citations,
          confidenceScore: confidence,
          createdAt: new Date().toISOString(),
        };

        if (!activeSessionId) {
          addSession({
            id: crypto.randomUUID(),
            title: content.slice(0, 60),
            messages: [userMsg, assistantMsg],
            createdAt: new Date().toISOString(),
          });
        } else {
          addMessage(activeSessionId, assistantMsg);
        }
      } catch (e) {
        if ((e as Error).name !== 'AbortError') setError('Stream failed');
      } finally {
        setIsStreaming(false);
        updateStreamingContent('');
      }
    } else {
      try {
        const { data } = await chatCompletion(content, activeSessionId ?? undefined);

        const assistantMsg: ChatMessage = {
          id: data.messageId,
          role: 'assistant',
          content: data.answer,
          citations: data.citations,
          confidenceScore: data.confidenceScore,
          createdAt: new Date().toISOString(),
        };

        if (!activeSessionId) {
          addSession({
            id: data.sessionId,
            title: content.slice(0, 60),
            messages: [userMsg, assistantMsg],
            createdAt: new Date().toISOString(),
          });
        } else {
          addMessage(activeSessionId, assistantMsg);
        }
      } catch {
        setError('Failed to get response');
      }
    }
  }, [activeSessionId, addSession, addMessage, setIsStreaming, updateStreamingContent]);

  const sendFeedback = useCallback(async (messageId: string, rating: number, comment?: string) => {
    if (!activeSessionId) return;
    try {
      await submitFeedback(messageId, activeSessionId, rating, comment);
      setMessageFeedback(activeSessionId, messageId, rating, comment);
    } catch { /* ignore */ }
  }, [activeSessionId, setMessageFeedback]);

  const stopStreaming = useCallback(() => {
    abortRef.current?.abort();
    setIsStreaming(false);
  }, [setIsStreaming]);

  const newChat = useCallback(() => setActiveSession(null), [setActiveSession]);

  return {
    activeSession, sessions, isStreaming, error,
    sendMessage, sendFeedback, stopStreaming, newChat, setActiveSession,
  };
}
