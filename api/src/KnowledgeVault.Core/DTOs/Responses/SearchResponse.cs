namespace KnowledgeVault.Core.DTOs.Responses;

public record SearchResponse(
    string Query,
    IReadOnlyList<SearchHit> Hits,
    int TotalResults,
    double ResponseTimeMs
);

public record SearchHit(
    Guid DocumentId,
    Guid ChunkId,
    string Content,
    string DocumentTitle,
    string? SectionHeading,
    double DenseScore,
    double SparseScore,
    double FusedScore,
    double RerankedScore
);

public record ChatCompletionResponse(
    Guid SessionId,
    Guid MessageId,
    string Answer,
    IReadOnlyList<CitationResponse> Citations,
    double ConfidenceScore,
    bool HasPotentialHallucinations
);

public record CitationResponse(
    Guid ChunkId,
    string DocumentTitle,
    string Excerpt,
    double Confidence,
    bool Verified
);

public record DocumentResponse(
    Guid Id,
    string Title,
    string ContentType,
    string Status,
    int Version,
    int ChunkCount,
    string? SourceUrl,
    DateTime CreatedAt,
    DateTime? IndexedAt
);

public record PaginatedResponse<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize
);

public record IngestionStatusResponse(
    Guid JobId,
    Guid DocumentId,
    string Status,
    string? ErrorMessage,
    int RetryCount,
    DateTime CreatedAt,
    DateTime? CompletedAt
);
