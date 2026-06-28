using KnowledgeVault.Application.Commands.Documents;
using KnowledgeVault.Application.Common;
using KnowledgeVault.Application.Queries.Documents;
using KnowledgeVault.Core.DTOs.Requests;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeVault.API.Controllers.V1;

[ApiController]
[Route("api/v1/documents")]
[Produces("application/json")]
public class DocumentsController(IMediator mediator, TenantContext tenant) : ControllerBase
{
    /// <summary>List documents for current tenant</summary>
    [HttpGet]
    [ProducesResponseType(200)]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await mediator.Send(new GetDocumentsQuery(tenant.TenantId, page, pageSize));
        return Ok(result);
    }

    /// <summary>Get document by ID</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Get(Guid id)
    {
        var result = await mediator.Send(new GetDocumentByIdQuery(tenant.TenantId, id));
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Create a new document</summary>
    [HttpPost]
    [ProducesResponseType(201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create([FromBody] CreateDocumentRequest request)
    {
        var result = await mediator.Send(new CreateDocumentCommand(
            tenant.TenantId, request.Title, request.ContentType, request.SourceUrl, null));
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    /// <summary>Upload and ingest a document file</summary>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(202)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Upload([FromForm] string title, [FromForm] IFormFile file)
    {
        using var stream = file.OpenReadStream();
        var result = await mediator.Send(new CreateDocumentCommand(
            tenant.TenantId, title, file.ContentType, null, stream));
        return Accepted(result);
    }

    /// <summary>Delete a document</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await mediator.Send(new DeleteDocumentCommand(tenant.TenantId, id));
        return deleted ? NoContent() : NotFound();
    }
}
