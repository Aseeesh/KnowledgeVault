namespace KnowledgeVault.Core.Entities;

public class Tenant
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public string? ApiKey { get; set; }
    public int RateLimitPerMinute { get; set; } = 60;
    public double DenseSearchWeight { get; set; } = 0.5;
    public double SparseSearchWeight { get; set; } = 0.5;
    public Dictionary<string, object> Settings { get; set; } = [];
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Document> Documents { get; set; } = [];
    public ICollection<ChatSession> ChatSessions { get; set; } = [];
}
