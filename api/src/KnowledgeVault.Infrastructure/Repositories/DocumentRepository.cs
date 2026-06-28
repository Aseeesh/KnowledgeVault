using Microsoft.EntityFrameworkCore;
using KnowledgeVault.Core.Entities;
using KnowledgeVault.Core.Enums;
using KnowledgeVault.Core.Interfaces.Repositories;
using KnowledgeVault.Infrastructure.Data;

namespace KnowledgeVault.Infrastructure.Repositories;

public class DocumentRepository(AppDbContext db) : IDocumentRepository
{
    public async Task<Document?> GetByIdAsync(Guid tenantId, Guid documentId, CancellationToken ct = default) =>
        await db.Documents.Include(d => d.Chunks)
            .FirstOrDefaultAsync(d => d.Id == documentId && d.TenantId == tenantId, ct);

    public async Task<(IReadOnlyList<Document> Items, int TotalCount)> GetByTenantAsync(
        Guid tenantId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.Documents.Where(d => d.TenantId == tenantId);
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }

    public async Task<Document> CreateAsync(Document document, CancellationToken ct = default)
    {
        db.Documents.Add(document);
        await db.SaveChangesAsync(ct);
        return document;
    }

    public async Task UpdateAsync(Document document, CancellationToken ct = default)
    {
        document.UpdatedAt = DateTime.UtcNow;
        db.Documents.Update(document);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid tenantId, Guid documentId, CancellationToken ct = default)
    {
        var doc = await db.Documents.FirstOrDefaultAsync(d => d.Id == documentId && d.TenantId == tenantId, ct);
        if (doc is not null) { db.Documents.Remove(doc); await db.SaveChangesAsync(ct); }
    }

    public async Task<IReadOnlyList<Document>> GetByStatusAsync(DocumentStatus status, int limit = 50, CancellationToken ct = default) =>
        await db.Documents.Where(d => d.Status == status).Take(limit).ToListAsync(ct);

    public async Task<IReadOnlyList<Document>> GetStaleDocumentsAsync(TimeSpan freshnessThreshold, CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow - freshnessThreshold;
        return await db.Documents
            .Where(d => d.Status == DocumentStatus.Indexed && (d.FreshnessCheckedAt == null || d.FreshnessCheckedAt < cutoff))
            .Take(50).ToListAsync(ct);
    }
}

public class DocumentChunkRepository(AppDbContext db) : IDocumentChunkRepository
{
    public async Task<IReadOnlyList<DocumentChunk>> GetByDocumentAsync(Guid documentId, CancellationToken ct = default) =>
        await db.DocumentChunks.Where(c => c.DocumentId == documentId).OrderBy(c => c.ChunkIndex).ToListAsync(ct);

    public async Task BulkInsertAsync(IEnumerable<DocumentChunk> chunks, CancellationToken ct = default)
    {
        db.DocumentChunks.AddRange(chunks);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteByDocumentAsync(Guid documentId, CancellationToken ct = default)
    {
        await db.DocumentChunks.Where(c => c.DocumentId == documentId).ExecuteDeleteAsync(ct);
    }

    public async Task<IReadOnlyList<DocumentChunk>> FullTextSearchAsync(
        Guid tenantId, string query, int topK = 20, CancellationToken ct = default)
    {
        var searchTerms = string.Join(" & ", query.Split(' ', StringSplitOptions.RemoveEmptyEntries));

        return await db.DocumentChunks
            .Where(c => c.TenantId == tenantId && EF.Functions.ToTsVector("english", c.Content)
                .Matches(EF.Functions.ToTsQuery("english", searchTerms)))
            .Take(topK)
            .ToListAsync(ct);
    }
}
