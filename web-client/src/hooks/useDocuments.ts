import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { listDocuments, createDocument, deleteDocument, uploadDocument } from '../services/api';

export function useDocuments(page = 1) {
  const qc = useQueryClient();

  const query = useQuery({
    queryKey: ['documents', page],
    queryFn: () => listDocuments(page).then((r) => r.data),
  });

  const createMutation = useMutation({
    mutationFn: (data: { title: string; contentType: string }) => createDocument(data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['documents'] }),
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => deleteDocument(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['documents'] }),
  });

  const uploadMutation = useMutation({
    mutationFn: ({ title, file }: { title: string; file: File }) => uploadDocument(title, file),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['documents'] }),
  });

  return { ...query, createDocument: createMutation, deleteDocument: deleteMutation, uploadDocument: uploadMutation };
}
