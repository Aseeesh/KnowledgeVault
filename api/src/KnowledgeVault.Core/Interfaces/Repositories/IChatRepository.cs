using KnowledgeVault.Core.Entities;

namespace KnowledgeVault.Core.Interfaces.Repositories;

public interface IChatRepository
{
    Task<ChatSession?> GetSessionAsync(Guid tenantId, Guid sessionId, CancellationToken ct = default);
    Task<IReadOnlyList<ChatSession>> GetSessionsByTenantAsync(Guid tenantId, int page, int pageSize, CancellationToken ct = default);
    Task<ChatSession> CreateSessionAsync(ChatSession session, CancellationToken ct = default);
    Task<ChatMessage> AddMessageAsync(ChatMessage message, CancellationToken ct = default);
    Task<UserFeedback> AddFeedbackAsync(UserFeedback feedback, CancellationToken ct = default);
}
