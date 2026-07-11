using Microsoft.AspNetCore.Mvc;
using KnowledgeVault.Application.Queries.Search;
using MediatR;

namespace KnowledgeVault.API.Controllers;

[ApiController]
[Route("api/v1/search")]
public class SearchController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<SearchController> _logger;

    public SearchController(IMediator mediator, ILogger<SearchController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> Search([FromBody] SearchRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Query))
                return BadRequest(new { error = "Query cannot be empty" });

            _logger.LogInformation("Search request: '{Query}', TopK: {TopK}", request.Query, request.TopK);

            var tenantId = HttpContext.Items["TenantId"]?.ToString();
            if (string.IsNullOrEmpty(tenantId))
            {
                return BadRequest(new { error = "Tenant ID is required" });
            }

            var query = new HybridSearchQuery(
                Guid.Parse(tenantId),
                request.Query,
                request.TopK ?? 10
            );

            var result = await _mediator.Send(query);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Search failed: {Message}", ex.Message);
            return StatusCode(500, new 
            { 
                error = "Search failed",
                details = ex.Message
            });
        }
    }
}

public class SearchRequest
{
    public string Query { get; set; } = string.Empty;
    public int? TopK { get; set; }
}