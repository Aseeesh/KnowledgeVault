using Qdrant.Client;
using Qdrant.Client.Grpc;
using KnowledgeVault.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace KnowledgeVault.Infrastructure.VectorStore;

public class QdrantVectorStoreService(QdrantClient client, ILogger<QdrantVectorStoreService> logger) : IVectorStoreService
{
    public async Task EnsureCollectionAsync(int vectorSize = 768, CancellationToken ct = default)
    {
        var collections = await client.ListCollectionsAsync(ct);
        if (collections.All(c => c != "document_chunks"))
        {
            await client.CreateCollectionAsync("document_chunks",
                new VectorParams { Size = (ulong)vectorSize, Distance = Distance.Cosine }, cancellationToken: ct);
            logger.LogInformation("Created Qdrant collection 'document_chunks' with dim={VectorSize}", vectorSize);
        }
    }

    public async Task UpsertAsync(string collectionName, IEnumerable<VectorPoint> points, CancellationToken ct = default)
    {
        var qdrantPoints = points.Select(p => new PointStruct
        {
            Id = new PointId { Uuid = p.Id },
            Vectors = p.Vector,
            Payload = { ["tenant_id"] = p.Payload.GetValueOrDefault("tenant_id")?.ToString() ?? "",
                        ["document_id"] = p.Payload.GetValueOrDefault("document_id")?.ToString() ?? "",
                        ["document_title"] = p.Payload.GetValueOrDefault("document_title")?.ToString() ?? "",
                        ["content"] = p.Payload.GetValueOrDefault("content")?.ToString() ?? "",
                        ["chunk_index"] = (long)(p.Payload.GetValueOrDefault("chunk_index") is int ci ? ci : 0),
                        ["section_heading"] = p.Payload.GetValueOrDefault("section_heading")?.ToString() ?? "" }
        }).ToList();

        await client.UpsertAsync(collectionName, qdrantPoints, cancellationToken: ct);
        logger.LogDebug("Upserted {Count} vectors to {Collection}", qdrantPoints.Count, collectionName);
    }

    public async Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        string collectionName, float[] queryVector, string tenantId, int topK = 20, CancellationToken ct = default)
    {
        var filter = new Filter
        {
            Must = { new Condition { Field = new FieldCondition
            {
                Key = "tenant_id",
                Match = new Match { Keyword = tenantId }
            }}}
        };

        var results = await client.QueryAsync(collectionName, queryVector, filter: filter,
            limit: (ulong)topK, cancellationToken: ct);

        return results.Select(r => new VectorSearchResult(
            r.Id.Uuid,
            r.Score,
            new Dictionary<string, object>
            {
                ["content"] = r.Payload.GetValueOrDefault("content")?.StringValue ?? "",
                ["document_id"] = r.Payload.GetValueOrDefault("document_id")?.StringValue ?? "",
                ["document_title"] = r.Payload.GetValueOrDefault("document_title")?.StringValue ?? "",
                ["section_heading"] = r.Payload.GetValueOrDefault("section_heading")?.StringValue ?? "",
                ["chunk_index"] = r.Payload.GetValueOrDefault("chunk_index")?.IntegerValue ?? 0
            }
        )).ToList();
    }

    public async Task DeleteByDocumentAsync(string collectionName, string documentId, CancellationToken ct = default)
    {
        var filter = new Filter
        {
            Must = { new Condition { Field = new FieldCondition
            {
                Key = "document_id",
                Match = new Match { Keyword = documentId }
            }}}
        };

        await client.DeleteAsync(collectionName, filter, cancellationToken: ct);
    }
}
