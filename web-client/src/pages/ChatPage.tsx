import { useRef, useEffect } from 'react';
import { Plus, MessageSquare, Loader2, Bot } from 'lucide-react';
import { clsx } from 'clsx';
import { useChat } from '../hooks/useChat';
import { useAppStore } from '../store';
import ChatInput from '../components/chat/ChatInput';
import MessageBubble from '../components/chat/MessageBubble';

export default function ChatPage() {
  const { activeSession, sessions, isStreaming, error, sendMessage, sendFeedback, stopStreaming, newChat, setActiveSession } = useChat();
  const streamingContent = useAppStore((s) => s.streamingContent);
  const theme = useAppStore((s) => s.theme);
  const messagesEndRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [activeSession?.messages, streamingContent]);

  const messages = activeSession?.messages ?? [];

  return (
    <div className="flex flex-1 overflow-hidden">
      {/* Session sidebar */}
      <div className={clsx(
        'w-56 border-r flex flex-col',
        theme === 'dark' ? 'bg-gray-900/50 border-gray-800' : 'bg-gray-50 border-gray-200'
      )}>
        <div className="p-3">
          <button
            onClick={newChat}
            className="flex items-center gap-2 w-full px-3 py-2 rounded-lg bg-indigo-600 hover:bg-indigo-700 text-white text-sm font-medium transition"
          >
            <Plus size={16} /> New Chat
          </button>
        </div>
        <div className="flex-1 overflow-y-auto p-2 space-y-1">
          {sessions.map((s) => (
            <button
              key={s.id}
              onClick={() => setActiveSession(s.id)}
              className={clsx(
                'w-full text-left px-3 py-2 rounded-lg text-xs truncate transition',
                s.id === activeSession?.id
                  ? 'bg-indigo-600/20 text-indigo-300'
                  : theme === 'dark' ? 'text-gray-400 hover:bg-gray-800' : 'text-gray-600 hover:bg-gray-100'
              )}
            >
              <MessageSquare size={12} className="inline mr-1.5" />
              {s.title || 'New conversation'}
            </button>
          ))}
        </div>
      </div>

      {/* Chat area */}
      <div className="flex-1 flex flex-col">
        {messages.length === 0 && !isStreaming ? (
          <div className="flex-1 flex items-center justify-center">
            <div className="text-center max-w-md">
              <div className="w-16 h-16 mx-auto mb-4 rounded-2xl bg-indigo-600/20 flex items-center justify-center">
                <Bot size={32} className="text-indigo-400" />
              </div>
              <h2 className="text-xl font-bold mb-2">KnowledgeVault Chat</h2>
              <p className={clsx('text-sm mb-6', theme === 'dark' ? 'text-gray-500' : 'text-gray-400')}>
                Ask questions about your documents. Answers are grounded in your knowledge base with verified citations.
              </p>
              <div className="grid grid-cols-2 gap-2">
                {['What is RAG?', 'How does hybrid search work?', 'Explain citation verification', 'What is BM25?'].map((q) => (
                  <button
                    key={q}
                    onClick={() => sendMessage(q)}
                    className={clsx(
                      'px-3 py-2 rounded-lg text-xs text-left transition',
                      theme === 'dark' ? 'bg-gray-800 hover:bg-gray-700 text-gray-300' : 'bg-gray-100 hover:bg-gray-200 text-gray-700'
                    )}
                  >
                    {q}
                  </button>
                ))}
              </div>
            </div>
          </div>
        ) : (
          <div className="flex-1 overflow-y-auto py-4">
            {messages.map((msg) => (
              <MessageBubble key={msg.id} message={msg} onFeedback={sendFeedback} />
            ))}
            {isStreaming && streamingContent && (
              <div className="flex gap-3 px-4 py-3">
                <div className="w-8 h-8 rounded-lg bg-indigo-600/20 flex items-center justify-center flex-shrink-0 mt-1">
                  <Loader2 size={16} className="text-indigo-400 animate-spin" />
                </div>
                <div className={clsx(
                  'max-w-[75%] rounded-xl px-4 py-3 text-sm',
                  theme === 'dark' ? 'bg-gray-800 border border-gray-700' : 'bg-gray-100 border border-gray-200'
                )}>
                  {streamingContent}
                  <span className="animate-pulse">|</span>
                </div>
              </div>
            )}
            <div ref={messagesEndRef} />
          </div>
        )}

        {error && (
          <div className="mx-4 mb-2 px-3 py-2 bg-red-500/10 border border-red-500/30 rounded-lg text-red-400 text-sm">
            {error}
          </div>
        )}

        <ChatInput onSend={sendMessage} onStop={stopStreaming} isStreaming={isStreaming} />
      </div>
    </div>
  );
}
