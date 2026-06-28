import { useState } from 'react';
import { Search, Loader2, Sparkles, Clock } from 'lucide-react';
import { clsx } from 'clsx';
import { hybridSearch } from '../services/api';
import { useAppStore } from '../store';
import type { SearchResponse } from '../types';

export default function SearchPage() {
  const [query, setQuery] = useState('');
  const [loading, setLoading] = useState(false);
  const [results, setResults] = useState<SearchResponse | null>(null);
  const theme = useAppStore((s) => s.theme);

  const handleSearch = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!query.trim()) return;
    setLoading(true);
    try {
      const { data } = await hybridSearch(query, 5);
      setResults(data);
    } catch {
      setResults(null);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="flex-1 overflow-y-auto p-8">
      <div className="max-w-3xl mx-auto">
        <h2 className="text-2xl font-bold mb-2">Hybrid Search</h2>
        <p className={clsx('text-sm mb-6', theme === 'dark' ? 'text-gray-500' : 'text-gray-400')}>
          Dense vectors + BM25 sparse retrieval, merged with Reciprocal Rank Fusion
        </p>

        <form onSubmit={handleSearch} className="relative mb-6">
          <input
            type="text"
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            placeholder="Search your knowledge base..."
            className={clsx(
              'w-full px-4 py-3.5 pl-12 rounded-xl border text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 transition',
              theme === 'dark' ? 'bg-gray-900 border-gray-700' : 'bg-white border-gray-300'
            )}
          />
          <Search className="absolute left-4 top-4 text-gray-500" size={18} />
          <button
            type="submit"
            disabled={loading || !query.trim()}
            className="absolute right-2 top-2 px-4 py-2 bg-indigo-600 hover:bg-indigo-700 disabled:opacity-40 rounded-lg text-sm font-medium text-white transition flex items-center gap-2"
          >
            {loading ? <Loader2 size={14} className="animate-spin" /> : <Sparkles size={14} />}
            Search
          </button>
        </form>

        {results && (
          <div>
            <div className={clsx('flex items-center gap-3 mb-4 text-xs', theme === 'dark' ? 'text-gray-500' : 'text-gray-400')}>
              <span>{results.totalResults} results</span>
              <span className="flex items-center gap-1"><Clock size={12} /> {results.responseTimeMs.toFixed(0)}ms</span>
            </div>
            {results.hits.length === 0 ? (
              <p className={clsx('text-center py-12', theme === 'dark' ? 'text-gray-600' : 'text-gray-400')}>
                No results found
              </p>
            ) : (
              <div className="space-y-3">
                {results.hits.map((hit, i) => (
                  <div
                    key={hit.chunkId + i}
                    className={clsx(
                      'rounded-lg border p-4 transition hover:border-indigo-500/50',
                      theme === 'dark' ? 'bg-gray-900 border-gray-800' : 'bg-white border-gray-200'
                    )}
                  >
                    <div className="flex justify-between items-start mb-2">
                      <span className={clsx('text-xs font-medium', theme === 'dark' ? 'text-indigo-400' : 'text-indigo-600')}>
                        {hit.documentTitle || 'Document'}
                      </span>
                      <div className="flex gap-2 text-xs">
                        <span className="text-emerald-400">Dense: {hit.denseScore.toFixed(3)}</span>
                        <span className="text-amber-400">Sparse: {hit.sparseScore.toFixed(3)}</span>
                        <span className="text-indigo-400 font-medium">Fused: {hit.fusedScore.toFixed(4)}</span>
                      </div>
                    </div>
                    {hit.sectionHeading && (
                      <p className={clsx('text-xs mb-1', theme === 'dark' ? 'text-gray-500' : 'text-gray-400')}>
                        Section: {hit.sectionHeading}
                      </p>
                    )}
                    <p className={clsx('text-sm leading-relaxed', theme === 'dark' ? 'text-gray-300' : 'text-gray-700')}>
                      {hit.content}
                    </p>
                  </div>
                ))}
              </div>
            )}
          </div>
        )}

        {!results && !loading && (
          <div className="text-center py-16">
            <Search size={48} className={clsx('mx-auto mb-4', theme === 'dark' ? 'text-gray-800' : 'text-gray-200')} />
            <p className={theme === 'dark' ? 'text-gray-600' : 'text-gray-400'}>Enter a query to search</p>
          </div>
        )}
      </div>
    </div>
  );
}
