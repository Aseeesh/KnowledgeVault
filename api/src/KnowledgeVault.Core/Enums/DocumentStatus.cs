namespace KnowledgeVault.Core.Enums;

public enum DocumentStatus
{
    Pending,
    Processing,
    Indexed,
    Failed,
    Stale
}

public enum IngestionJobType
{
    Upload,
    Reindex,
    FreshnessCheck
}

public enum IngestionJobStatus
{
    Queued,
    Processing,
    Completed,
    Failed,
    DeadLettered
}
