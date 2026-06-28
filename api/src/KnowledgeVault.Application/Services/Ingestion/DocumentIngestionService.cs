using KnowledgeVault.Core.Entities;
using KnowledgeVault.Core.Enums;
using KnowledgeVault.Core.Interfaces.Repositories;
using KnowledgeVault.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace KnowledgeVault.Application.Services.Ingestion;

public class DocumentIngestionService(
    IDocumentRepository documentRepo,
    IDocumentChunkRepository chunkRepo,
    IIngestionJobRepository jobRepo,
    IChunkingService chunkingService,
    IEmbeddingService embeddingService,
    IVectorStoreService vectorStore,
    ILogger<DocumentIngestionService> logger) : IIngestionService
{
    public async Task<IngestionJob> EnqueueDocumentAsync(
        Guid tenantId, Guid documentId, Stream content, string contentType, CancellationToken ct = default)
    {
        var job = new IngestionJob
        {
            TenantId = tenantId,
            DocumentId = documentId,
            JobType = IngestionJobType.Upload
        };
        return await jobRepo.CreateAsync(job, ct);
    }

    public async Task ProcessDocumentAsync(Guid jobId, CancellationToken ct = default)
    {
        var job = await jobRepo.GetByIdAsync(jobId, ct);
        if (job is null) return;

        try
        {
            job.Status = IngestionJobStatus.Processing;
            job.StartedAt = DateTime.UtcNow;
            await jobRepo.UpdateAsync(job, ct);

            var document = await documentRepo.GetByIdAsync(job.TenantId, job.DocumentId, ct);
            if (document is null)
                throw new InvalidOperationException($"Document {job.DocumentId} not found");

            document.Status = DocumentStatus.Processing;
            await documentRepo.UpdateAsync(document, ct);

            // For now, use a placeholder text — in production, download from storage and parse
            var sampleText = $"Document: {document.Title}. This is the content of the document that would be extracted from the uploaded file.";
            var chunks = chunkingService.ChunkText(sampleText);

            await chunkRepo.DeleteByDocumentAsync(document.Id, ct);

            var dbChunks = chunks.Select(c => new DocumentChunk
            {
                DocumentId = document.Id,
                TenantId = job.TenantId,
                ChunkIndex = c.Index,
                Content = c.Content,
                TokenCount = c.TokenCount,
                SectionHeading = c.SectionHeading
            }).ToList();
            await chunkRepo.BulkInsertAsync(dbChunks, ct);

            var texts = chunks.Select(c => c.Content).ToList();
            var embeddings = await embeddingService.EmbedBatchAsync(texts, ct);

            var vectorPoints = dbChunks.Zip(embeddings, (chunk, embedding) => new VectorPoint(
                chunk.Id.ToString(),
                embedding,
                new Dictionary<string, object>
                {
                    ["tenant_id"] = job.TenantId.ToString(),
                    ["document_id"] = document.Id.ToString(),
                    ["document_title"] = document.Title,
                    ["content"] = chunk.Content,
                    ["chunk_index"] = chunk.ChunkIndex,
                    ["section_heading"] = chunk.SectionHeading ?? ""
                }
            )).ToList();

            await vectorStore.UpsertAsync("document_chunks", vectorPoints, ct);

            document.Status = DocumentStatus.Indexed;
            document.IndexedAt = DateTime.UtcNow;
            document.ChunkCount = chunks.Count;
            await documentRepo.UpdateAsync(document, ct);

            job.Status = IngestionJobStatus.Completed;
            job.CompletedAt = DateTime.UtcNow;
            await jobRepo.UpdateAsync(job, ct);

            logger.LogInformation("Document {DocumentId} ingested: {ChunkCount} chunks, {VectorCount} vectors",
                document.Id, chunks.Count, vectorPoints.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ingestion failed for job {JobId}", jobId);

            job.Status = job.RetryCount >= job.MaxRetries
                ? IngestionJobStatus.DeadLettered
                : IngestionJobStatus.Failed;
            job.ErrorMessage = ex.Message;
            job.RetryCount++;
            await jobRepo.UpdateAsync(job, ct);

            var document = await documentRepo.GetByIdAsync(job.TenantId, job.DocumentId, ct);
            if (document is not null)
            {
                document.Status = DocumentStatus.Failed;
                document.ErrorMessage = ex.Message;
                await documentRepo.UpdateAsync(document, ct);
            }

            throw;
        }
    }
}
