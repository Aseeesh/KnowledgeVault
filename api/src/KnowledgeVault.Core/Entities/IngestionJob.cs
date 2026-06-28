using KnowledgeVault.Core.Enums;

namespace KnowledgeVault.Core.Entities;

public class IngestionJob
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid DocumentId { get; set; }
    public IngestionJobType JobType { get; set; }
    public IngestionJobStatus Status { get; set; } = IngestionJobStatus.Queued;
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    public int MaxRetries { get; set; } = 3;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Document Document { get; set; } = null!;
}
