import { useState, useRef, useEffect } from 'react';
import { Send, StopCircle } from 'lucide-react';
import { clsx } from 'clsx';
import { useAppStore } from '../../store';

interface Props {
  onSend: (message: string, stream?: boolean) => void;
  onStop: () => void;
  isStreaming: boolean;
}

export default function ChatInput({ onSend, onStop, isStreaming }: Props) {
  const [input, setInput] = useState('');
  const textareaRef = useRef<HTMLTextAreaElement>(null);
  const theme = useAppStore((s) => s.theme);

  useEffect(() => {
    if (textareaRef.current) {
      textareaRef.current.style.height = 'auto';
      textareaRef.current.style.height = Math.min(textareaRef.current.scrollHeight, 200) + 'px';
    }
  }, [input]);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!input.trim() || isStreaming) return;
    onSend(input.trim());
    setInput('');
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSubmit(e);
    }
  };

  return (
    <form onSubmit={handleSubmit} className={clsx(
      'border-t p-4',
      theme === 'dark' ? 'border-gray-800 bg-gray-900/50' : 'border-gray-200 bg-gray-50'
    )}>
      <div className={clsx(
        'flex items-end gap-2 rounded-xl border p-2',
        theme === 'dark' ? 'bg-gray-900 border-gray-700' : 'bg-white border-gray-300'
      )}>
        <textarea
          ref={textareaRef}
          value={input}
          onChange={(e) => setInput(e.target.value)}
          onKeyDown={handleKeyDown}
          placeholder="Ask anything about your documents... (Shift+Enter for new line)"
          rows={1}
          className={clsx(
            'flex-1 resize-none bg-transparent px-2 py-1.5 text-sm focus:outline-none',
            theme === 'dark' ? 'placeholder-gray-500' : 'placeholder-gray-400'
          )}
        />
        {isStreaming ? (
          <button type="button" onClick={onStop} className="p-2 text-red-400 hover:text-red-300 transition">
            <StopCircle size={20} />
          </button>
        ) : (
          <button
            type="submit"
            disabled={!input.trim()}
            className="p-2 text-indigo-400 hover:text-indigo-300 disabled:opacity-30 transition"
          >
            <Send size={20} />
          </button>
        )}
      </div>
      <p className={clsx('text-xs mt-2 text-center', theme === 'dark' ? 'text-gray-600' : 'text-gray-400')}>
        Responses are generated using RAG with citation verification
      </p>
    </form>
  );
}
