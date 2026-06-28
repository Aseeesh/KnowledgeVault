using KnowledgeVault.Core.Entities;

namespace KnowledgeVault.Core.Interfaces.Services;

public interface IIngestionService
{
    Task<IngestionJob> EnqueueDocumentAsync(Guid tenantId, Guid documentId, Stream content, string contentType, CancellationToken ct = default);
    Task ProcessDocumentAsync(Guid jobId, CancellationToken ct = default);
}

public interface IDocumentParser
{
    bool CanParse(string contentType);
    Task<ParsedDocument> ParseAsync(Stream content, string contentType, CancellationToken ct = default);
}

public record ParsedDocument(string Text, IReadOnlyList<DocumentSection> Sections);
public record DocumentSection(string? Heading, string Content, int Level);

public interface IChunkingService
{
    IReadOnlyList<TextChunk> ChunkText(string text, IReadOnlyList<DocumentSection>? sections = null, int chunkSize = 512, int overlap = 50);
}

public record TextChunk(string Content, int Index, string? SectionHeading, int TokenCount);

public interface IMessageQueueService
{
    Task PublishAsync<T>(string queueName, T message, CancellationToken ct = default);
}
