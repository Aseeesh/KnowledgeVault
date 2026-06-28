using KnowledgeVault.Core.DTOs.Responses;

namespace KnowledgeVault.Core.Interfaces.Services;

public interface ISearchService
{
    Task<SearchResponse> HybridSearchAsync(Guid tenantId, string query, int topK = 10, Dictionary<string, string>? filters = null, CancellationToken ct = default);
}

public interface IVectorStoreService
{
    Task EnsureCollectionAsync(int vectorSize = 768, CancellationToken ct = default);
    Task UpsertAsync(string collectionName, IEnumerable<VectorPoint> points, CancellationToken ct = default);
    Task<IReadOnlyList<VectorSearchResult>> SearchAsync(string collectionName, float[] queryVector, string tenantId, int topK = 20, CancellationToken ct = default);
    Task DeleteByDocumentAsync(string collectionName, string documentId, CancellationToken ct = default);
}

public record VectorPoint(string Id, float[] Vector, Dictionary<string, object> Payload);
public record VectorSearchResult(string Id, float Score, Dictionary<string, object> Payload);

public interface IEmbeddingService
{
    Task<float[]> EmbedAsync(string text, CancellationToken ct = default);
    Task<IReadOnlyList<float[]>> EmbedBatchAsync(IReadOnlyList<string> texts, CancellationToken ct = default);
}

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default);
    Task RemoveAsync(string key, CancellationToken ct = default);
    Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default);
}
