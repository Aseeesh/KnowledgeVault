using System.Net.Http.Json;
using KnowledgeVault.Core.DTOs.Responses;
using KnowledgeVault.Core.Entities;
using KnowledgeVault.Core.Interfaces.Repositories;
using KnowledgeVault.Core.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace KnowledgeVault.Application.Commands.Chat;

public record ChatCompletionCommand(
    Guid TenantId,
    string Message,
    Guid? SessionId,
    string? UserId
) : IRequest<ChatCompletionResponse>;

public class ChatCompletionHandler(
    ISearchService searchService,
    ICitationService citationService,
    IChatRepository chatRepo,
    IHttpClientFactory httpClientFactory,
    ILogger<ChatCompletionHandler> logger)
    : IRequestHandler<ChatCompletionCommand, ChatCompletionResponse>
{
    public async Task<ChatCompletionResponse> Handle(ChatCompletionCommand request, CancellationToken ct)
    {
        var session = request.SessionId.HasValue
            ? await chatRepo.GetSessionAsync(request.TenantId, request.SessionId.Value, ct)
            : null;

        if (session is null)
        {
            session = await chatRepo.CreateSessionAsync(new ChatSession
            {
                TenantId = request.TenantId,
                UserId = request.UserId,
                Title = request.Message.Length > 80 ? request.Message[..80] + "..." : request.Message
            }, ct);
        }

        await chatRepo.AddMessageAsync(new ChatMessage
        {
            SessionId = session.Id,
            Role = "user",
            Content = request.Message
        }, ct);

        var searchResults = await searchService.HybridSearchAsync(request.TenantId, request.Message, topK: 5, ct: ct);

        var sourceChunks = searchResults.Hits.Select(h => new RetrievedChunk(
            h.ChunkId, h.Content, h.DocumentTitle, h.FusedScore)).ToList();

        var client = httpClientFactory.CreateClient("AIEngine");
        var aiResponse = await client.PostAsJsonAsync("generate/", new
        {
            query = request.Message,
            context_chunks = sourceChunks.Select(c => c.Content).ToList()
        }, ct);

        var aiResult = await aiResponse.Content.ReadFromJsonAsync<AiGenerateResult>(ct);
        var answer = aiResult?.Answer ?? "I couldn't generate an answer.";

        var verification = await citationService.VerifyCitationsAsync(answer, sourceChunks, ct);

        var citations = verification.Citations.Select(c => new CitationResponse(
            c.ChunkId, c.DocumentTitle, c.Excerpt, c.ConfidenceScore, c.IsVerified)).ToList();

        var assistantMsg = await chatRepo.AddMessageAsync(new ChatMessage
        {
            SessionId = session.Id,
            Role = "assistant",
            Content = answer,
            ConfidenceScore = verification.OverallConfidence,
            Citations = verification.Citations.Select(c => new CitationReference
            {
                ChunkId = c.ChunkId,
                DocumentTitle = c.DocumentTitle,
                Excerpt = c.Excerpt,
                ConfidenceScore = c.ConfidenceScore,
                IsVerified = c.IsVerified
            }).ToList()
        }, ct);

        logger.LogInformation("Chat completion for session {SessionId}: confidence={Confidence:F2}, citations={CitationCount}",
            session.Id, verification.OverallConfidence, citations.Count);

        return new ChatCompletionResponse(
            session.Id, assistantMsg.Id, answer, citations,
            verification.OverallConfidence, verification.HasPotentialHallucinations);
    }

    private record AiGenerateResult(string Answer, List<AiCitation>? Citations);
    private record AiCitation(int ChunkIndex, string Text, double Confidence);
}
