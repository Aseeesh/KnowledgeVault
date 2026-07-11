using KnowledgeVault.Application.Commands.Documents;
using KnowledgeVault.Application.Common;
using KnowledgeVault.Application.Queries.Documents;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeVault.API.Controllers.V1;

[ApiController]
[Route("api/v1/documents")]
[Produces("application/json")]
public class DocumentsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly TenantContext _tenant;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(IMediator mediator, TenantContext tenant, ILogger<DocumentsController> logger)
    {
        _mediator = mediator;
        _tenant = tenant;
        _logger = logger;
    }

    /// <summary>Get all documents for tenant with pagination</summary>
    [HttpGet]
    [ProducesResponseType(200)]
    public async Task<IActionResult> GetDocuments([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var result = await _mediator.Send(new GetDocumentsQuery(_tenant.TenantId, page, pageSize));
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get documents");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>Get document by ID</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetDocument(Guid id)
    {
        try
        {
            var result = await _mediator.Send(new GetDocumentByIdQuery(_tenant.TenantId, id));
            if (result is null)
                return NotFound(new { error = "Document not found" });
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get document {DocumentId}", id);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>Create a new document</summary>
    [HttpPost]
    [ProducesResponseType(201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreateDocument([FromBody] CreateDocumentRequest request)
    {
        try
        {
            var result = await _mediator.Send(new CreateDocumentCommand(
                _tenant.TenantId,
                request.Title,
                request.ContentType,
                request.SourceUrl,
                null));
            return CreatedAtAction(nameof(GetDocument), new { id = result.Id }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create document");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>Upload a document file</summary>
    [HttpPost("upload")]
    [ProducesResponseType(201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> UploadDocument([FromForm] IFormFile file, [FromForm] string title)
    {
        try
        {
            if (file is null || file.Length == 0)
                return BadRequest(new { error = "File is required" });

            var content = new MemoryStream();
            await file.CopyToAsync(content);
            content.Position = 0;

            var result = await _mediator.Send(new CreateDocumentCommand(
                _tenant.TenantId,
                title ?? file.FileName,
                file.ContentType,
                null,
                content));

            return CreatedAtAction(nameof(GetDocument), new { id = result.Id }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload document");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>Delete a document</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeleteDocument(Guid id)
    {
        try
        {
            var result = await _mediator.Send(new DeleteDocumentCommand(_tenant.TenantId, id));
            if (!result)
                return NotFound(new { error = "Document not found" });
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete document {DocumentId}", id);
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

public class CreateDocumentRequest
{
    public string Title { get; set; } = string.Empty;
    public string ContentType { get; set; } = "text/plain";
    public string? SourceUrl { get; set; }
}