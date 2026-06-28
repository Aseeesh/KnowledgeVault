namespace KnowledgeVault.Core.Entities;

public class ChatSession
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string? UserId { get; set; }
    public string? Title { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Tenant Tenant { get; set; } = null!;
    public ICollection<ChatMessage> Messages { get; set; } = [];
}

public class ChatMessage
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public required string Role { get; set; }
    public required string Content { get; set; }
    public List<CitationReference> Citations { get; set; } = [];
    public double? ConfidenceScore { get; set; }
    public int? TokensUsed { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ChatSession Session { get; set; } = null!;
    public UserFeedback? Feedback { get; set; }
}

public class CitationReference
{
    public Guid ChunkId { get; set; }
    public string DocumentTitle { get; set; } = "";
    public string Excerpt { get; set; } = "";
    public double ConfidenceScore { get; set; }
    public bool IsVerified { get; set; }
}
