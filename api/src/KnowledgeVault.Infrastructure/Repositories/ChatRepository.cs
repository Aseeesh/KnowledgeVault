using Microsoft.EntityFrameworkCore;
using KnowledgeVault.Core.Entities;
using KnowledgeVault.Core.Interfaces.Repositories;
using KnowledgeVault.Infrastructure.Data;

namespace KnowledgeVault.Infrastructure.Repositories;

public class ChatRepository(AppDbContext db) : IChatRepository
{
    public async Task<ChatSession?> GetSessionAsync(Guid tenantId, Guid sessionId, CancellationToken ct = default) =>
        await db.ChatSessions.Include(s => s.Messages).ThenInclude(m => m.Feedback)
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.TenantId == tenantId, ct);

    public async Task<IReadOnlyList<ChatSession>> GetSessionsByTenantAsync(
        Guid tenantId, int page, int pageSize, CancellationToken ct = default) =>
        await db.ChatSessions.Where(s => s.TenantId == tenantId)
            .OrderByDescending(s => s.UpdatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

    public async Task<ChatSession> CreateSessionAsync(ChatSession session, CancellationToken ct = default)
    {
        db.ChatSessions.Add(session);
        await db.SaveChangesAsync(ct);
        return session;
    }

    public async Task<ChatMessage> AddMessageAsync(ChatMessage message, CancellationToken ct = default)
    {
        db.ChatMessages.Add(message);
        await db.SaveChangesAsync(ct);
        return message;
    }

    public async Task<UserFeedback> AddFeedbackAsync(UserFeedback feedback, CancellationToken ct = default)
    {
        db.UserFeedbacks.Add(feedback);
        await db.SaveChangesAsync(ct);
        return feedback;
    }
}
