using System.Net.Http.Json;
using KnowledgeVault.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace KnowledgeVault.Infrastructure.Search;

public class EmbeddingService(IHttpClientFactory httpClientFactory, ICacheService cache, ILogger<EmbeddingService> logger) : IEmbeddingService
{
    public async Task<float[]> EmbedAsync(string text, CancellationToken ct = default)
    {
        var cacheKey = $"embed:{text.GetHashCode()}";
        var cached = await cache.GetAsync<float[]>(cacheKey, ct);
        if (cached is not null) return cached;

        var client = httpClientFactory.CreateClient("AIEngine");
        var response = await client.PostAsJsonAsync("embeddings/", new { texts = new[] { text } }, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(ct);
        var embedding = result?.Embeddings?.FirstOrDefault()
            ?? throw new InvalidOperationException("No embedding returned");

        await cache.SetAsync(cacheKey, embedding, TimeSpan.FromHours(24), ct);
        return embedding;
    }

    public async Task<IReadOnlyList<float[]>> EmbedBatchAsync(IReadOnlyList<string> texts, CancellationToken ct = default)
    {
        var client = httpClientFactory.CreateClient("AIEngine");
        var response = await client.PostAsJsonAsync("embeddings/", new { texts }, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(ct);
        return result?.Embeddings ?? throw new InvalidOperationException("No embeddings returned");
    }

    private record EmbeddingResponse(List<float[]> Embeddings, string Model, int Dimensions);
}
