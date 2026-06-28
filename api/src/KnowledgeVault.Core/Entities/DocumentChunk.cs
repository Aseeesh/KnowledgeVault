namespace KnowledgeVault.Core.Entities;

public class DocumentChunk
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public Guid TenantId { get; set; }
    public int ChunkIndex { get; set; }
    public required string Content { get; set; }
    public int TokenCount { get; set; }
    public string? VectorId { get; set; }
    public string? SectionHeading { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = [];
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Document Document { get; set; } = null!;
}
