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
public class ChatController(IMediator mediator, TenantContext tenant, IChatRepository chatRepo) : ControllerBase
{
    /// <summary>Send a chat message and get RAG-powered response</summary>
    [HttpPost("completions")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Complete([FromBody] ChatCompletionRequest request)
    {
        var result = await mediator.Send(new ChatCompletionCommand(
            tenant.TenantId, request.Message, request.SessionId, null));
        return Ok(result);
    }

    /// <summary>Stream a chat completion response via SSE</summary>
    [HttpPost("completions/stream")]
    public async Task Stream([FromBody] ChatCompletionRequest request, CancellationToken ct)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";

        var result = await mediator.Send(new ChatCompletionCommand(
            tenant.TenantId, request.Message, request.SessionId, null), ct);

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

    /// <summary>Get chat session with message history</summary>
    [HttpGet("sessions/{sessionId:guid}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetSession(Guid sessionId)
    {
        var session = await chatRepo.GetSessionAsync(tenant.TenantId, sessionId);
        return session is null ? NotFound() : Ok(session);
    }

    /// <summary>Submit feedback for a chat message</summary>
    [HttpPost("messages/{messageId:guid}/feedback")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> Feedback(Guid messageId, [FromBody] FeedbackRequest request)
    {
        var feedback = await chatRepo.AddFeedbackAsync(new Core.Entities.UserFeedback
        {
            SessionId = request.SessionId,
            MessageId = messageId,
            Rating = request.Rating,
            Comment = request.Comment
        });
        return Ok(feedback);
    }
}

public record FeedbackRequest(Guid SessionId, int Rating, string? Comment);
