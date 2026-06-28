using KnowledgeVault.Core.Interfaces.Repositories;
using KnowledgeVault.Core.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace KnowledgeVault.Application.Commands.Documents;

public record DeleteDocumentCommand(Guid TenantId, Guid DocumentId) : IRequest<bool>;

public class DeleteDocumentHandler(
    IDocumentRepository documentRepo,
    IDocumentChunkRepository chunkRepo,
    IVectorStoreService vectorStore,
    ILogger<DeleteDocumentHandler> logger)
    : IRequestHandler<DeleteDocumentCommand, bool>
{
    public async Task<bool> Handle(DeleteDocumentCommand request, CancellationToken ct)
    {
        var doc = await documentRepo.GetByIdAsync(request.TenantId, request.DocumentId, ct);
        if (doc is null) return false;

        await vectorStore.DeleteByDocumentAsync("document_chunks", request.DocumentId.ToString(), ct);
        await chunkRepo.DeleteByDocumentAsync(request.DocumentId, ct);
        await documentRepo.DeleteAsync(request.TenantId, request.DocumentId, ct);

        logger.LogInformation("Document {DocumentId} deleted from tenant {TenantId}", request.DocumentId, request.TenantId);
        return true;
    }
}
