using System.Text.RegularExpressions;
using KnowledgeVault.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace KnowledgeVault.Application.Services.Citation;

public partial class CitationVerificationService(
    IEmbeddingService embeddingService,
    ILogger<CitationVerificationService> logger) : ICitationService
{
    public async Task<CitationVerificationResult> VerifyCitationsAsync(
        string response, IReadOnlyList<RetrievedChunk> sourceChunks, CancellationToken ct = default)
    {
        var claims = ExtractClaims(response);
        var citations = new List<VerifiedCitation>();
        var unsupportedClaims = new List<string>();

        foreach (var claim in claims)
        {
            var (bestChunk, similarity) = await FindBestMatchingChunk(claim, sourceChunks, ct);

            if (bestChunk is not null && similarity >= 0.5)
            {
                citations.Add(new VerifiedCitation(
                    bestChunk.ChunkId,
                    bestChunk.DocumentTitle,
                    bestChunk.Content.Length > 200 ? bestChunk.Content[..200] + "..." : bestChunk.Content,
                    claim,
                    similarity,
                    similarity >= 0.7
                ));
            }
            else
            {
                unsupportedClaims.Add(claim);
            }
        }

        var parsedRefs = ExtractSourceReferences(response);
        foreach (var refIndex in parsedRefs)
        {
            if (refIndex < sourceChunks.Count)
            {
                var chunk = sourceChunks[refIndex];
                if (citations.All(c => c.ChunkId != chunk.ChunkId))
                {
                    citations.Add(new VerifiedCitation(
                        chunk.ChunkId, chunk.DocumentTitle,
                        chunk.Content.Length > 200 ? chunk.Content[..200] + "..." : chunk.Content,
                        $"Referenced as [Source {refIndex + 1}]",
                        chunk.RetrievalScore, true));
                }
            }
        }

        double overallConfidence = citations.Count > 0
            ? citations.Average(c => c.ConfidenceScore)
            : 0.0;

        bool hasHallucinations = unsupportedClaims.Count > claims.Count * 0.3;

        logger.LogInformation("Citation verification: {CitationCount} verified, {UnsupportedCount} unsupported, confidence={Confidence:F2}",
            citations.Count, unsupportedClaims.Count, overallConfidence);

        return new CitationVerificationResult(
            overallConfidence, citations, unsupportedClaims, hasHallucinations);
    }

    private static List<string> ExtractClaims(string response)
    {
        return response
            .Split(['.', '!', '?'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(s => s.Length > 20)
            .Where(s => !s.StartsWith("I ") && !s.StartsWith("Based on") && !s.StartsWith("According"))
            .ToList();
    }

    private async Task<(RetrievedChunk? Chunk, double Similarity)> FindBestMatchingChunk(
        string claim, IReadOnlyList<RetrievedChunk> chunks, CancellationToken ct)
    {
        if (chunks.Count == 0)
            return (null, 0.0);

        var claimEmbedding = await embeddingService.EmbedAsync(claim, ct);

        RetrievedChunk? bestChunk = null;
        double bestSimilarity = 0;

        var chunkTexts = chunks.Select(c => c.Content).ToList();
        var chunkEmbeddings = await embeddingService.EmbedBatchAsync(chunkTexts, ct);

        for (int i = 0; i < chunks.Count; i++)
        {
            var similarity = CosineSimilarity(claimEmbedding, chunkEmbeddings[i]);
            if (similarity > bestSimilarity)
            {
                bestSimilarity = similarity;
                bestChunk = chunks[i];
            }
        }

        return (bestChunk, bestSimilarity);
    }

    private static double CosineSimilarity(float[] a, float[] b)
    {
        double dot = 0, magA = 0, magB = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            magA += a[i] * a[i];
            magB += b[i] * b[i];
        }
        return dot / (Math.Sqrt(magA) * Math.Sqrt(magB) + 1e-10);
    }

    private static List<int> ExtractSourceReferences(string response)
    {
        return SourceRefPattern().Matches(response)
            .Select(m => int.Parse(m.Groups[1].Value) - 1)
            .Distinct()
            .ToList();
    }

    [GeneratedRegex(@"\[Source\s+(\d+)\]")]
    private static partial Regex SourceRefPattern();
}
