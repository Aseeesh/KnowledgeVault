namespace KnowledgeVault.Core.DTOs.Requests;

public record CreateDocumentRequest(
    string Title,
    string ContentType,
    string? SourceUrl = null,
    Dictionary<string, object>? Metadata = null
);

public record UpdateDocumentRequest(
    string? Title = null,
    string? SourceUrl = null,
    Dictionary<string, object>? Metadata = null
);

public record SearchRequest(
    string Query,
    int TopK = 10,
    Dictionary<string, string>? Filters = null
);

public record ChatCompletionRequest(
    string Message,
    Guid? SessionId = null
);
