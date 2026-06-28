namespace KnowledgeVault.Core.Entities;

public class UserFeedback
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public Guid MessageId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ChatMessage Message { get; set; } = null!;
}
