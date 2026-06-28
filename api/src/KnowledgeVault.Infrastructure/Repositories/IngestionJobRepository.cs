using Microsoft.EntityFrameworkCore;
using KnowledgeVault.Core.Entities;
using KnowledgeVault.Core.Enums;
using KnowledgeVault.Core.Interfaces.Repositories;
using KnowledgeVault.Infrastructure.Data;

namespace KnowledgeVault.Infrastructure.Repositories;

public class IngestionJobRepository(AppDbContext db) : IIngestionJobRepository
{
    public async Task<IngestionJob> CreateAsync(IngestionJob job, CancellationToken ct = default)
    {
        db.IngestionJobs.Add(job);
        await db.SaveChangesAsync(ct);
        return job;
    }

    public async Task<IngestionJob?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.IngestionJobs.FindAsync([id], ct);

    public async Task UpdateAsync(IngestionJob job, CancellationToken ct = default)
    {
        db.IngestionJobs.Update(job);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<IngestionJob>> GetByStatusAsync(
        IngestionJobStatus status, int limit = 10, CancellationToken ct = default) =>
        await db.IngestionJobs.Where(j => j.Status == status)
            .OrderBy(j => j.CreatedAt).Take(limit).ToListAsync(ct);

    public async Task<IReadOnlyList<IngestionJob>> GetByDocumentAsync(Guid documentId, CancellationToken ct = default) =>
        await db.IngestionJobs.Where(j => j.DocumentId == documentId)
            .OrderByDescending(j => j.CreatedAt).ToListAsync(ct);
}
