import { useState } from 'react';
import { User, Bot, ThumbsUp, ThumbsDown, Copy, Check, Shield, AlertTriangle } from 'lucide-react';
import { clsx } from 'clsx';
import ReactMarkdown from 'react-markdown';
import type { ChatMessage } from '../../types';
import CitationBadge from './CitationBadge';
import { useAppStore } from '../../store';

interface Props {
  message: ChatMessage;
  onFeedback: (messageId: string, rating: number) => void;
}

export default function MessageBubble({ message, onFeedback }: Props) {
  const [copied, setCopied] = useState(false);
  const theme = useAppStore((s) => s.theme);
  const isUser = message.role === 'user';

  const copyContent = () => {
    navigator.clipboard.writeText(message.content);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  return (
    <div className={clsx('flex gap-3 px-4 py-3', isUser ? 'justify-end' : 'justify-start')}>
      {!isUser && (
        <div className="w-8 h-8 rounded-lg bg-indigo-600/20 flex items-center justify-center flex-shrink-0 mt-1">
          <Bot size={16} className="text-indigo-400" />
        </div>
      )}

      <div className={clsx('max-w-[75%] rounded-xl px-4 py-3', isUser
        ? 'bg-indigo-600 text-white'
        : theme === 'dark' ? 'bg-gray-800 border border-gray-700' : 'bg-gray-100 border border-gray-200'
      )}>
        <div className="prose prose-sm prose-invert max-w-none">
          <ReactMarkdown
            components={{
              code: ({ children, className }) => {
                if (className) {
                  return (
                    <div className="relative group">
                      <pre className={clsx(
                        'rounded-lg p-3 text-xs overflow-x-auto my-2',
                        theme === 'dark' ? 'bg-gray-900' : 'bg-gray-200'
                      )}>
                        <code>{children}</code>
                      </pre>
                      <button
                        onClick={() => navigator.clipboard.writeText(String(children))}
                        className="absolute top-2 right-2 opacity-0 group-hover:opacity-100 p-1 rounded bg-gray-700 text-gray-300 transition"
                      >
                        <Copy size={12} />
                      </button>
                    </div>
                  );
                }
                return <code className={clsx(
                  'px-1.5 py-0.5 rounded text-xs',
                  theme === 'dark' ? 'bg-gray-700' : 'bg-gray-200'
                )}>{children}</code>;
              },
              p: ({ children }) => <p className="mb-2 last:mb-0 text-sm leading-relaxed">{children}</p>,
            }}
          >
            {message.content}
          </ReactMarkdown>
        </div>

        {/* Citations */}
        {message.citations && message.citations.length > 0 && (
          <div className={clsx(
            'mt-3 pt-3 border-t',
            theme === 'dark' ? 'border-gray-700' : 'border-gray-200'
          )}>
            <div className="flex items-center gap-2 mb-2">
              <Shield size={12} className="text-indigo-400" />
              <span className={clsx('text-xs font-medium', theme === 'dark' ? 'text-gray-400' : 'text-gray-500')}>
                Sources ({message.citations.length})
              </span>
              {message.confidenceScore !== undefined && (
                <span className={clsx('text-xs px-1.5 py-0.5 rounded-full',
                  message.confidenceScore >= 0.7 ? 'bg-emerald-500/20 text-emerald-400'
                    : message.confidenceScore >= 0.4 ? 'bg-amber-500/20 text-amber-400'
                      : 'bg-red-500/20 text-red-400'
                )}>
                  {(message.confidenceScore * 100).toFixed(0)}% confidence
                </span>
              )}
            </div>
            <div className="flex flex-wrap gap-1">
              {message.citations.map((c, i) => (
                <CitationBadge key={c.chunkId + i} citation={c} index={i} />
              ))}
            </div>
          </div>
        )}

        {/* Actions for assistant messages */}
        {!isUser && (
          <div className={clsx(
            'flex items-center gap-1 mt-2 pt-2 border-t',
            theme === 'dark' ? 'border-gray-700' : 'border-gray-200'
          )}>
            <button
              onClick={copyContent}
              className={clsx('p-1 rounded transition', theme === 'dark' ? 'text-gray-500 hover:text-gray-300' : 'text-gray-400 hover:text-gray-600')}
              title="Copy"
            >
              {copied ? <Check size={14} className="text-emerald-400" /> : <Copy size={14} />}
            </button>
            <button
              onClick={() => onFeedback(message.id, 5)}
              className={clsx('p-1 rounded transition',
                message.feedback?.rating === 5 ? 'text-emerald-400' : theme === 'dark' ? 'text-gray-500 hover:text-emerald-400' : 'text-gray-400 hover:text-emerald-500'
              )}
              title="Helpful"
            >
              <ThumbsUp size={14} />
            </button>
            <button
              onClick={() => onFeedback(message.id, 1)}
              className={clsx('p-1 rounded transition',
                message.feedback?.rating === 1 ? 'text-red-400' : theme === 'dark' ? 'text-gray-500 hover:text-red-400' : 'text-gray-400 hover:text-red-500'
              )}
              title="Not helpful"
            >
              <ThumbsDown size={14} />
            </button>
            {message.confidenceScore !== undefined && message.confidenceScore < 0.4 && (
              <span className="flex items-center gap-1 text-xs text-amber-400 ml-2">
                <AlertTriangle size={12} /> Low confidence
              </span>
            )}
          </div>
        )}
      </div>

      {isUser && (
        <div className="w-8 h-8 rounded-lg bg-gray-600 flex items-center justify-center flex-shrink-0 mt-1">
          <User size={16} className="text-gray-300" />
        </div>
      )}
    </div>
  );
}
