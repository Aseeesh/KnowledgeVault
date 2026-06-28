using KnowledgeVault.Core.Entities;
using KnowledgeVault.Core.Enums;

namespace KnowledgeVault.Core.Interfaces.Repositories;

public interface IIngestionJobRepository
{
    Task<IngestionJob> CreateAsync(IngestionJob job, CancellationToken ct = default);
    Task<IngestionJob?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task UpdateAsync(IngestionJob job, CancellationToken ct = default);
    Task<IReadOnlyList<IngestionJob>> GetByStatusAsync(IngestionJobStatus status, int limit = 10, CancellationToken ct = default);
    Task<IReadOnlyList<IngestionJob>> GetByDocumentAsync(Guid documentId, CancellationToken ct = default);
}
