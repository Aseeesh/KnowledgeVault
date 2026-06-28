using KnowledgeVault.Application.Common;
using KnowledgeVault.Application.Queries.Search;
using KnowledgeVault.Core.DTOs.Requests;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeVault.API.Controllers.V1;

[ApiController]
[Route("api/v1/search")]
[Produces("application/json")]
public class SearchController(IMediator mediator, TenantContext tenant) : ControllerBase
{
    /// <summary>Hybrid search across tenant documents</summary>
    [HttpPost]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Search([FromBody] SearchRequest request)
    {
        var result = await mediator.Send(new HybridSearchQuery(
            tenant.TenantId, request.Query, request.TopK, request.Filters));
        return Ok(result);
    }
}
