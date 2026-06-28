import { useState } from 'react';
import { CheckCircle, AlertCircle, X } from 'lucide-react';
import { clsx } from 'clsx';
import type { Citation } from '../../types';
import { useAppStore } from '../../store';

interface Props {
  citation: Citation;
  index: number;
}

export default function CitationBadge({ citation, index }: Props) {
  const [expanded, setExpanded] = useState(false);
  const theme = useAppStore((s) => s.theme);

  return (
    <span className="relative inline-block">
      <button
        onClick={() => setExpanded(!expanded)}
        className={clsx(
          'inline-flex items-center justify-center w-5 h-5 rounded-full text-[10px] font-bold mx-0.5 cursor-pointer transition-all',
          citation.verified
            ? 'bg-emerald-500/20 text-emerald-400 hover:bg-emerald-500/30'
            : 'bg-amber-500/20 text-amber-400 hover:bg-amber-500/30'
        )}
        title={`${citation.documentTitle} (${(citation.confidence * 100).toFixed(0)}% confidence)`}
      >
        {index + 1}
      </button>

      {expanded && (
        <div className={clsx(
          'absolute bottom-full left-0 mb-2 w-80 rounded-lg border shadow-xl p-3 z-50 text-sm',
          theme === 'dark' ? 'bg-gray-800 border-gray-700' : 'bg-white border-gray-200'
        )}>
          <div className="flex items-start justify-between mb-2">
            <div className="flex items-center gap-1.5">
              {citation.verified
                ? <CheckCircle size={14} className="text-emerald-400" />
                : <AlertCircle size={14} className="text-amber-400" />}
              <span className="font-medium text-xs">
                {citation.verified ? 'Verified' : 'Unverified'} — {(citation.confidence * 100).toFixed(0)}%
              </span>
            </div>
            <button onClick={() => setExpanded(false)} className="text-gray-500 hover:text-gray-300">
              <X size={14} />
            </button>
          </div>
          <p className={clsx('font-medium text-xs mb-1', theme === 'dark' ? 'text-indigo-400' : 'text-indigo-600')}>
            {citation.documentTitle}
          </p>
          <p className={clsx('text-xs leading-relaxed', theme === 'dark' ? 'text-gray-400' : 'text-gray-600')}>
            "{citation.excerpt}"
          </p>
        </div>
      )}
    </span>
  );
}
