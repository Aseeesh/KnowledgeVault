using KnowledgeVault.Core.DTOs.Responses;
using KnowledgeVault.Core.Interfaces.Repositories;
using MediatR;

namespace KnowledgeVault.Application.Queries.Documents;

public record GetDocumentsQuery(Guid TenantId, int Page = 1, int PageSize = 20)
    : IRequest<PaginatedResponse<DocumentResponse>>;

public class GetDocumentsHandler(IDocumentRepository repo)
    : IRequestHandler<GetDocumentsQuery, PaginatedResponse<DocumentResponse>>
{
    public async Task<PaginatedResponse<DocumentResponse>> Handle(GetDocumentsQuery request, CancellationToken ct)
    {
        var (items, total) = await repo.GetByTenantAsync(request.TenantId, request.Page, request.PageSize, ct);

        var responses = items.Select(d => new DocumentResponse(
            d.Id, d.Title, d.ContentType, d.Status.ToString(),
            d.Version, d.ChunkCount, d.SourceUrl, d.CreatedAt, d.IndexedAt
        )).ToList();

        return new PaginatedResponse<DocumentResponse>(responses, total, request.Page, request.PageSize);
    }
}

public record GetDocumentByIdQuery(Guid TenantId, Guid DocumentId) : IRequest<DocumentResponse?>;

public class GetDocumentByIdHandler(IDocumentRepository repo)
    : IRequestHandler<GetDocumentByIdQuery, DocumentResponse?>
{
    public async Task<DocumentResponse?> Handle(GetDocumentByIdQuery request, CancellationToken ct)
    {
        var d = await repo.GetByIdAsync(request.TenantId, request.DocumentId, ct);
        if (d is null) return null;

        return new DocumentResponse(
            d.Id, d.Title, d.ContentType, d.Status.ToString(),
            d.Version, d.ChunkCount, d.SourceUrl, d.CreatedAt, d.IndexedAt);
    }
}
