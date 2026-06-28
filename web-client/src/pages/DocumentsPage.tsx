import { useState } from 'react';
import { FileText, Trash2, RefreshCw, ChevronLeft, ChevronRight } from 'lucide-react';
import { clsx } from 'clsx';
import { useDocuments } from '../hooks/useDocuments';
import { useAppStore } from '../store';
import FileUpload from '../components/documents/FileUpload';

const statusColors: Record<string, string> = {
  Pending: 'bg-amber-500/20 text-amber-400',
  Processing: 'bg-blue-500/20 text-blue-400',
  Indexed: 'bg-emerald-500/20 text-emerald-400',
  Failed: 'bg-red-500/20 text-red-400',
  Stale: 'bg-gray-500/20 text-gray-400',
};

export default function DocumentsPage() {
  const [page, setPage] = useState(1);
  const { data, isLoading, uploadDocument, deleteDocument } = useDocuments(page);
  const theme = useAppStore((s) => s.theme);

  const handleUpload = async (title: string, file: File) => {
    await uploadDocument.mutateAsync({ title, file });
  };

  const totalPages = data ? Math.ceil(data.totalCount / data.pageSize) : 1;

  return (
    <div className="flex-1 overflow-y-auto p-8">
      <div className="max-w-4xl mx-auto">
        <div className="flex justify-between items-start mb-6">
          <div>
            <h2 className="text-2xl font-bold">Documents</h2>
            <p className={clsx('text-sm mt-1', theme === 'dark' ? 'text-gray-500' : 'text-gray-400')}>
              {data?.totalCount ?? 0} documents in knowledge base
            </p>
          </div>
        </div>

        <FileUpload onUpload={handleUpload} />

        <div className="mt-6 space-y-2">
          {isLoading ? (
            <div className="text-center py-12">
              <RefreshCw size={24} className="mx-auto animate-spin text-gray-500" />
            </div>
          ) : data?.items.length === 0 ? (
            <div className="text-center py-12">
              <FileText size={48} className={clsx('mx-auto mb-4', theme === 'dark' ? 'text-gray-800' : 'text-gray-200')} />
              <p className={theme === 'dark' ? 'text-gray-600' : 'text-gray-400'}>No documents yet</p>
            </div>
          ) : (
            data?.items.map((doc) => (
              <div
                key={doc.id}
                className={clsx(
                  'flex items-center justify-between p-4 rounded-lg border transition',
                  theme === 'dark' ? 'bg-gray-900 border-gray-800 hover:border-gray-700' : 'bg-white border-gray-200 hover:border-gray-300'
                )}
              >
                <div className="flex items-center gap-3 min-w-0">
                  <FileText size={20} className="text-indigo-400 flex-shrink-0" />
                  <div className="min-w-0">
                    <p className="font-medium text-sm truncate">{doc.title}</p>
                    <div className={clsx('flex items-center gap-3 text-xs mt-0.5', theme === 'dark' ? 'text-gray-500' : 'text-gray-400')}>
                      <span>{doc.contentType}</span>
                      <span>v{doc.version}</span>
                      {doc.chunkCount > 0 && <span>{doc.chunkCount} chunks</span>}
                      <span>{new Date(doc.createdAt).toLocaleDateString()}</span>
                    </div>
                  </div>
                </div>
                <div className="flex items-center gap-2 flex-shrink-0">
                  <span className={clsx('text-xs px-2 py-1 rounded-full', statusColors[doc.status] ?? statusColors.Pending)}>
                    {doc.status}
                  </span>
                  <button
                    onClick={() => deleteDocument.mutate(doc.id)}
                    className="p-1.5 rounded text-gray-500 hover:text-red-400 transition"
                    title="Delete"
                  >
                    <Trash2 size={14} />
                  </button>
                </div>
              </div>
            ))
          )}
        </div>

        {totalPages > 1 && (
          <div className="flex justify-center items-center gap-4 mt-6">
            <button
              onClick={() => setPage((p) => Math.max(1, p - 1))}
              disabled={page === 1}
              className="p-1 disabled:opacity-30"
            >
              <ChevronLeft size={20} />
            </button>
            <span className="text-sm">Page {page} of {totalPages}</span>
            <button
              onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
              disabled={page === totalPages}
              className="p-1 disabled:opacity-30"
            >
              <ChevronRight size={20} />
            </button>
          </div>
        )}
      </div>
    </div>
  );
}
