import { useCallback, useState } from 'react';
import { useDropzone } from 'react-dropzone';
import { Upload, Loader2, CheckCircle } from 'lucide-react';
import { clsx } from 'clsx';
import { useAppStore } from '../../store';

interface Props {
  onUpload: (title: string, file: File) => Promise<void>;
}

export default function FileUpload({ onUpload }: Props) {
  const [uploading, setUploading] = useState(false);
  const [uploadedFile, setUploadedFile] = useState<string | null>(null);
  const theme = useAppStore((s) => s.theme);

  const onDrop = useCallback(async (files: File[]) => {
    const file = files[0];
    if (!file) return;

    setUploading(true);
    try {
      const title = file.name.replace(/\.[^/.]+$/, '');
      await onUpload(title, file);
      setUploadedFile(file.name);
      setTimeout(() => setUploadedFile(null), 3000);
    } catch { /* handled by caller */ } finally {
      setUploading(false);
    }
  }, [onUpload]);

  const { getRootProps, getInputProps, isDragActive } = useDropzone({
    onDrop,
    accept: {
      'application/pdf': ['.pdf'],
      'application/vnd.openxmlformats-officedocument.wordprocessingml.document': ['.docx'],
      'text/plain': ['.txt'],
      'text/html': ['.html'],
      'text/markdown': ['.md'],
    },
    maxFiles: 1,
    disabled: uploading,
  });

  return (
    <div
      {...getRootProps()}
      className={clsx(
        'border-2 border-dashed rounded-xl p-8 text-center cursor-pointer transition-all',
        isDragActive
          ? 'border-indigo-500 bg-indigo-500/10'
          : theme === 'dark'
            ? 'border-gray-700 hover:border-gray-500 bg-gray-900/30'
            : 'border-gray-300 hover:border-gray-400 bg-gray-50',
        uploading && 'pointer-events-none opacity-60'
      )}
    >
      <input {...getInputProps()} />

      {uploading ? (
        <div className="flex flex-col items-center gap-2">
          <Loader2 size={32} className="text-indigo-400 animate-spin" />
          <p className="text-sm">Processing document...</p>
        </div>
      ) : uploadedFile ? (
        <div className="flex flex-col items-center gap-2">
          <CheckCircle size={32} className="text-emerald-400" />
          <p className="text-sm text-emerald-400">"{uploadedFile}" uploaded successfully</p>
        </div>
      ) : (
        <div className="flex flex-col items-center gap-2">
          <Upload size={32} className={theme === 'dark' ? 'text-gray-500' : 'text-gray-400'} />
          <p className="text-sm">
            {isDragActive ? 'Drop file here...' : 'Drag & drop a file, or click to browse'}
          </p>
          <p className={clsx('text-xs', theme === 'dark' ? 'text-gray-600' : 'text-gray-400')}>
            PDF, DOCX, TXT, HTML, Markdown
          </p>
        </div>
      )}
    </div>
  );
}
