namespace KnowledgeVault.Core.Events;

public record DocumentIngestionEvent(
    Guid JobId,
    Guid TenantId,
    Guid DocumentId,
    string JobType,
    DateTime CreatedAt
);

public record DocumentIndexedEvent(
    Guid TenantId,
    Guid DocumentId,
    int ChunkCount,
    DateTime IndexedAt
);

public record DocumentFailedEvent(
    Guid TenantId,
    Guid DocumentId,
    Guid JobId,
    string ErrorMessage,
    int RetryCount
);
