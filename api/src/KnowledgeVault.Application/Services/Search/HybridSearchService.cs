using System.Diagnostics;
using KnowledgeVault.Core.DTOs.Responses;
using KnowledgeVault.Core.Interfaces.Repositories;
using KnowledgeVault.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace KnowledgeVault.Application.Services.Search;

public class HybridSearchService(
    IVectorStoreService vectorStore,
    IEmbeddingService embeddingService,
    IDocumentChunkRepository chunkRepo,
    ICacheService cache,
    ITenantRepository tenantRepo,
    ILogger<HybridSearchService> logger) : ISearchService
{
    public async Task<SearchResponse> HybridSearchAsync(
        Guid tenantId, string query, int topK = 10,
        Dictionary<string, string>? filters = null, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();

        var cacheKey = $"search:{tenantId}:{query.GetHashCode()}:{topK}";
        var cached = await cache.GetAsync<SearchResponse>(cacheKey, ct);
        if (cached is not null)
        {
            logger.LogDebug("Search cache hit for query '{Query}'", query);
            return cached;
        }

        var tenant = await tenantRepo.GetByIdAsync(tenantId, ct);
        var denseWeight = tenant?.DenseSearchWeight ?? 0.5;
        var sparseWeight = tenant?.SparseSearchWeight ?? 0.5;

        var queryEmbedding = await embeddingService.EmbedAsync(query, ct);

        var denseTask = vectorStore.SearchAsync("document_chunks", queryEmbedding, tenantId.ToString(), topK * 2, ct);
        var sparseTask = chunkRepo.FullTextSearchAsync(tenantId, query, topK * 2, ct);

        await Task.WhenAll(denseTask, sparseTask);

        var denseResults = await denseTask;
        var sparseResults = await sparseTask;

        var fused = ReciprocalRankFusion(denseResults, sparseResults, denseWeight, sparseWeight);

        var reranked = await CrossEncoderRerank(query, fused, ct);

        var hits = reranked.Take(topK).Select((r, rank) => new SearchHit(
            Guid.TryParse(r.Payload.GetValueOrDefault("document_id")?.ToString(), out var did) ? did : Guid.Empty,
            Guid.TryParse(r.Id, out var cid) ? cid : Guid.Empty,
            r.Payload.GetValueOrDefault("content")?.ToString() ?? "",
            r.Payload.GetValueOrDefault("document_title")?.ToString() ?? "",
            r.Payload.GetValueOrDefault("section_heading")?.ToString(),
            r.DenseScore,
            r.SparseScore,
            r.FusedScore,
            r.RerankedScore
        )).ToList();

        sw.Stop();
        var response = new SearchResponse(query, hits, hits.Count, sw.Elapsed.TotalMilliseconds);

        await cache.SetAsync(cacheKey, response, TimeSpan.FromMinutes(5), ct);

        logger.LogInformation("Hybrid search for '{Query}': {HitCount} results in {ElapsedMs:F0}ms (dense={DenseCount}, sparse={SparseCount})",
            query, hits.Count, sw.Elapsed.TotalMilliseconds, denseResults.Count, sparseResults.Count);

        return response;
    }

    private static List<FusedResult> ReciprocalRankFusion(
        IReadOnlyList<VectorSearchResult> denseResults,
        IReadOnlyList<Core.Entities.DocumentChunk> sparseResults,
        double denseWeight, double sparseWeight, int k = 60)
    {
        var scores = new Dictionary<string, FusedResult>();

        for (int rank = 0; rank < denseResults.Count; rank++)
        {
            var r = denseResults[rank];
            var id = r.Id;
            if (!scores.ContainsKey(id))
                scores[id] = new FusedResult(id, r.Payload, 0, 0, 0, 0);

            var entry = scores[id];
            scores[id] = entry with
            {
                DenseScore = r.Score,
                FusedScore = entry.FusedScore + denseWeight * (1.0 / (k + rank + 1))
            };
        }

        for (int rank = 0; rank < sparseResults.Count; rank++)
        {
            var r = sparseResults[rank];
            var id = r.Id.ToString();
            if (!scores.ContainsKey(id))
            {
                var payload = new Dictionary<string, object>
                {
                    ["content"] = r.Content,
                    ["document_id"] = r.DocumentId.ToString(),
                    ["document_title"] = r.SectionHeading ?? "",
                    ["section_heading"] = r.SectionHeading ?? ""
                };
                scores[id] = new FusedResult(id, payload, 0, 0, 0, 0);
            }

            var entry = scores[id];
            scores[id] = entry with
            {
                SparseScore = 1.0 / (rank + 1),
                FusedScore = entry.FusedScore + sparseWeight * (1.0 / (k + rank + 1))
            };
        }

        return scores.Values.OrderByDescending(r => r.FusedScore).ToList();
    }

    private Task<List<FusedResult>> CrossEncoderRerank(string query, List<FusedResult> candidates, CancellationToken ct)
    {
        // Cross-encoder reranking placeholder — in production, call a mini cross-encoder model
        // For now, use fused score as the reranked score
        var reranked = candidates
            .Select(c => c with { RerankedScore = c.FusedScore })
            .OrderByDescending(c => c.RerankedScore)
            .ToList();
        return Task.FromResult(reranked);
    }

    internal record FusedResult(
        string Id,
        Dictionary<string, object> Payload,
        double DenseScore,
        double SparseScore,
        double FusedScore,
        double RerankedScore
    );
}
