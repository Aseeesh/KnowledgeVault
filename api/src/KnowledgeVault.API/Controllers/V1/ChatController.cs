using KnowledgeVault.Application.Commands.Chat;
using KnowledgeVault.Application.Common;
using KnowledgeVault.Core.DTOs.Requests;
using KnowledgeVault.Core.Interfaces.Repositories;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeVault.API.Controllers.V1;

[ApiController]
[Route("api/v1/chat")]
[Produces("application/json")]
public class ChatController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly TenantContext _tenant;
    private readonly IChatRepository _chatRepo;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        IMediator mediator, 
        TenantContext tenant, 
        IChatRepository chatRepo,
        ILogger<ChatController> logger)
    {
        _mediator = mediator;
        _tenant = tenant;
        _chatRepo = chatRepo;
        _logger = logger;
    }

    /// <summary>Send a chat message and get RAG-powered response</summary>
    [HttpPost("completions")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> Complete([FromBody] ChatCompletionRequest request)
    {
        try
        {
            _logger.LogInformation("Chat completion request for tenant {TenantId}, message length: {Length}", 
                _tenant.TenantId, request.Message?.Length ?? 0);

            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { error = "Message cannot be empty" });
            }

            var result = await _mediator.Send(new ChatCompletionCommand(
                _tenant.TenantId, 
                request.Message, 
                request.SessionId, 
                null));

            _logger.LogInformation("Chat completion successful for session {SessionId}, confidence: {Confidence}", 
                result.SessionId, result.ConfidenceScore);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chat completion failed: {Message}", ex.Message);
            return StatusCode(500, new 
            { 
                error = "Chat completion failed", 
                details = ex.Message,
                type = ex.GetType().Name
            });
        }
    }

    /// <summary>Stream a chat completion response via SSE</summary>
    [HttpPost("completions/stream")]
    public async Task Stream([FromBody] ChatCompletionRequest request, CancellationToken ct)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";

        try
        {
            var result = await _mediator.Send(new ChatCompletionCommand(
                _tenant.TenantId, 
                request.Message, 
                request.SessionId, 
                null), ct);

            // Stream the response word by word for SSE
            var words = result.Answer.Split(' ');
            foreach (var word in words)
            {
                if (ct.IsCancellationRequested) break;
                await Response.WriteAsync($"data: {word} \n\n", ct);
                await Response.Body.FlushAsync(ct);
                await Task.Delay(30, ct);
            }

            // Send citations at the end
            await Response.WriteAsync($"data: [CITATIONS]{System.Text.Json.JsonSerializer.Serialize(result.Citations)}\n\n", ct);
            await Response.WriteAsync($"data: [CONFIDENCE]{result.ConfidenceScore:F2}\n\n", ct);
            await Response.WriteAsync("data: [DONE]\n\n", ct);
            await Response.Body.FlushAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Stream chat failed: {Message}", ex.Message);
            await Response.WriteAsync($"data: [ERROR]{ex.Message}\n\n", ct);
            await Response.Body.FlushAsync(ct);
        }
    }

    /// <summary>Get chat session with message history</summary>
    [HttpGet("sessions/{sessionId:guid}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetSession(Guid sessionId)
    {
        try
        {
            var session = await _chatRepo.GetSessionAsync(_tenant.TenantId, sessionId);
            if (session is null)
                return NotFound(new { error = "Session not found" });
            return Ok(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get session {SessionId}", sessionId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>Submit feedback for a chat message</summary>
    [HttpPost("messages/{messageId:guid}/feedback")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Feedback(Guid messageId, [FromBody] FeedbackRequest request)
    {
        try
        {
            if (request.Rating < 1 || request.Rating > 5)
                return BadRequest(new { error = "Rating must be between 1 and 5" });

            var feedback = await _chatRepo.AddFeedbackAsync(new Core.Entities.UserFeedback
            {
                SessionId = request.SessionId,
                MessageId = messageId,
                Rating = request.Rating,
                Comment = request.Comment
            });
            return Ok(feedback);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add feedback for message {MessageId}", messageId);
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

public record FeedbackRequest(Guid SessionId, int Rating, string? Comment);