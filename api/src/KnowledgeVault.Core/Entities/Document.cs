using KnowledgeVault.Core.Enums;

namespace KnowledgeVault.Core.Entities;

public class Document
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public required string Title { get; set; }
    public string? SourceUrl { get; set; }
    public required string ContentType { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Pending;
    public string? Checksum { get; set; }
    public long? SizeBytes { get; set; }
    public int Version { get; set; } = 1;
    public string? ErrorMessage { get; set; }
    public int ChunkCount { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = [];
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? IndexedAt { get; set; }
    public DateTime? FreshnessCheckedAt { get; set; }

    public Tenant Tenant { get; set; } = null!;
    public ICollection<DocumentChunk> Chunks { get; set; } = [];
}
