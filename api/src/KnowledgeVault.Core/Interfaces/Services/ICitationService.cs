namespace KnowledgeVault.Core.Interfaces.Services;

public interface ICitationService
{
    Task<CitationVerificationResult> VerifyCitationsAsync(string response, IReadOnlyList<RetrievedChunk> sourceChunks, CancellationToken ct = default);
}

public record RetrievedChunk(Guid ChunkId, string Content, string DocumentTitle, double RetrievalScore);

public record CitationVerificationResult(
    double OverallConfidence,
    IReadOnlyList<VerifiedCitation> Citations,
    IReadOnlyList<string> UnsupportedClaims,
    bool HasPotentialHallucinations
);

public record VerifiedCitation(
    Guid ChunkId,
    string DocumentTitle,
    string Excerpt,
    string ClaimText,
    double ConfidenceScore,
    bool IsVerified
);
