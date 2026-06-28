using KnowledgeVault.Core.Entities;
using KnowledgeVault.Core.Enums;

namespace KnowledgeVault.Core.Interfaces.Repositories;

public interface IDocumentRepository
{
    Task<Document?> GetByIdAsync(Guid tenantId, Guid documentId, CancellationToken ct = default);
    Task<(IReadOnlyList<Document> Items, int TotalCount)> GetByTenantAsync(Guid tenantId, int page, int pageSize, CancellationToken ct = default);
    Task<Document> CreateAsync(Document document, CancellationToken ct = default);
    Task UpdateAsync(Document document, CancellationToken ct = default);
    Task DeleteAsync(Guid tenantId, Guid documentId, CancellationToken ct = default);
    Task<IReadOnlyList<Document>> GetByStatusAsync(DocumentStatus status, int limit = 50, CancellationToken ct = default);
    Task<IReadOnlyList<Document>> GetStaleDocumentsAsync(TimeSpan freshnessThreshold, CancellationToken ct = default);
}

public interface IDocumentChunkRepository
{
    Task<IReadOnlyList<DocumentChunk>> GetByDocumentAsync(Guid documentId, CancellationToken ct = default);
    Task BulkInsertAsync(IEnumerable<DocumentChunk> chunks, CancellationToken ct = default);
    Task DeleteByDocumentAsync(Guid documentId, CancellationToken ct = default);
    Task<IReadOnlyList<DocumentChunk>> FullTextSearchAsync(Guid tenantId, string query, int topK = 20, CancellationToken ct = default);
}
