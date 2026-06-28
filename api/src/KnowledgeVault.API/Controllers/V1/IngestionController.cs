using KnowledgeVault.Application.Common;
using KnowledgeVault.Core.DTOs.Responses;
using KnowledgeVault.Core.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeVault.API.Controllers.V1;

[ApiController]
[Route("api/v1/ingestion")]
[Produces("application/json")]
public class IngestionController(IIngestionJobRepository jobRepo, TenantContext tenant) : ControllerBase
{
    /// <summary>Get ingestion job status</summary>
    [HttpGet("jobs/{jobId:guid}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetJob(Guid jobId)
    {
        var job = await jobRepo.GetByIdAsync(jobId);
        if (job is null || job.TenantId != tenant.TenantId) return NotFound();

        return Ok(new IngestionStatusResponse(
            job.Id, job.DocumentId, job.Status.ToString(),
            job.ErrorMessage, job.RetryCount, job.CreatedAt, job.CompletedAt));
    }

    /// <summary>List ingestion jobs for a document</summary>
    [HttpGet("documents/{documentId:guid}/jobs")]
    public async Task<IActionResult> GetDocumentJobs(Guid documentId)
    {
        var jobs = await jobRepo.GetByDocumentAsync(documentId);
        var responses = jobs
            .Where(j => j.TenantId == tenant.TenantId)
            .Select(j => new IngestionStatusResponse(
                j.Id, j.DocumentId, j.Status.ToString(),
                j.ErrorMessage, j.RetryCount, j.CreatedAt, j.CompletedAt));
        return Ok(responses);
    }
}
