using KnowledgeVault.Core.DTOs.Responses;
using KnowledgeVault.Core.Entities;
using KnowledgeVault.Core.Enums;
using KnowledgeVault.Core.Interfaces.Repositories;
using KnowledgeVault.Core.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace KnowledgeVault.Application.Commands.Documents;

public record CreateDocumentCommand(
    Guid TenantId,
    string Title,
    string ContentType,
    string? SourceUrl,
    Stream? FileContent
) : IRequest<DocumentResponse>;

public class CreateDocumentHandler(
    IDocumentRepository documentRepo,
    IIngestionJobRepository jobRepo,
    IMessageQueueService messageQueue,
    ILogger<CreateDocumentHandler> logger)
    : IRequestHandler<CreateDocumentCommand, DocumentResponse>
{
    public async Task<DocumentResponse> Handle(CreateDocumentCommand request, CancellationToken ct)
    {
        var document = new Document
        {
            TenantId = request.TenantId,
            Title = request.Title,
            ContentType = request.ContentType,
            SourceUrl = request.SourceUrl,
            Status = request.FileContent is not null ? DocumentStatus.Pending : DocumentStatus.Pending
        };

        var created = await documentRepo.CreateAsync(document, ct);
        logger.LogInformation("Document {DocumentId} created for tenant {TenantId}", created.Id, request.TenantId);

        if (request.FileContent is not null)
        {
            var job = new IngestionJob
            {
                TenantId = request.TenantId,
                DocumentId = created.Id,
                JobType = IngestionJobType.Upload
            };
            await jobRepo.CreateAsync(job, ct);

            await messageQueue.PublishAsync("document.ingest", new Core.Events.DocumentIngestionEvent(
                job.Id, request.TenantId, created.Id, "Upload", DateTime.UtcNow), ct);

            logger.LogInformation("Ingestion job {JobId} enqueued for document {DocumentId}", job.Id, created.Id);
        }

        return new DocumentResponse(
            created.Id, created.Title, created.ContentType,
            created.Status.ToString(), created.Version, 0,
            created.SourceUrl, created.CreatedAt, null);
    }
}
