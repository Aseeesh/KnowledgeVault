using KnowledgeVault.Core.DTOs.Responses;
using KnowledgeVault.Core.Interfaces.Services;
using MediatR;

namespace KnowledgeVault.Application.Queries.Search;

public record HybridSearchQuery(
    Guid TenantId,
    string Query,
    int TopK = 10,
    Dictionary<string, string>? Filters = null
) : IRequest<SearchResponse>;

public class HybridSearchHandler(ISearchService searchService)
    : IRequestHandler<HybridSearchQuery, SearchResponse>
{
    public Task<SearchResponse> Handle(HybridSearchQuery request, CancellationToken ct)
        => searchService.HybridSearchAsync(request.TenantId, request.Query, request.TopK, request.Filters, ct);
}
