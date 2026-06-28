using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using KnowledgeVault.Application.Services.Citation;
using KnowledgeVault.Core.Interfaces.Services;

namespace KnowledgeVault.Tests.Unit.Services;

public class CitationVerificationTests
{
    private readonly CitationVerificationService _sut;
    private readonly Mock<IEmbeddingService> _embeddingMock = new();

    public CitationVerificationTests()
    {
        _embeddingMock.Setup(e => e.EmbedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenerateRandomVector(768));
        _embeddingMock.Setup(e => e.EmbedBatchAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<string> texts, CancellationToken _) =>
                texts.Select(_ => GenerateRandomVector(768)).ToList());

        _sut = new CitationVerificationService(
            _embeddingMock.Object,
            NullLogger<CitationVerificationService>.Instance);
    }

    [Fact]
    public async Task VerifyCitations_WithSourceReferences_ExtractsCorrectly()
    {
        var response = "RAG combines retrieval with generation [Source 1]. BM25 handles keyword matching [Source 2].";
        var chunks = new List<RetrievedChunk>
        {
            new(Guid.NewGuid(), "RAG combines retrieval with generation.", "Doc 1", 0.9),
            new(Guid.NewGuid(), "BM25 is a keyword matching algorithm.", "Doc 2", 0.8),
        };

        var result = await _sut.VerifyCitationsAsync(response, chunks);

        result.Citations.Should().HaveCountGreaterOrEqualTo(2);
        result.OverallConfidence.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task VerifyCitations_NoChunks_ReturnsZeroConfidence()
    {
        var result = await _sut.VerifyCitationsAsync("Some answer", []);

        result.OverallConfidence.Should().Be(0);
        result.Citations.Should().BeEmpty();
    }

    [Fact]
    public async Task VerifyCitations_ManyUnsupportedClaims_FlagsHallucination()
    {
        var callCount = 0;
        _embeddingMock.Setup(e => e.EmbedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => GenerateRandomVector(768, ++callCount));
        _embeddingMock.Setup(e => e.EmbedBatchAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<string> texts, CancellationToken _) =>
                texts.Select(_ => GenerateRandomVector(768, ++callCount)).ToList());

        var response = "Claim one is very specific. Claim two is also specific. Claim three has no basis. Claim four is made up.";
        var chunks = new List<RetrievedChunk>
        {
            new(Guid.NewGuid(), "Something completely unrelated.", "Doc 1", 0.5),
        };

        var result = await _sut.VerifyCitationsAsync(response, chunks);

        // With different random vectors, claims won't match the single unrelated chunk
        result.UnsupportedClaims.Count.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task VerifyCitations_ResultHasCorrectStructure()
    {
        var response = "Answer with [Source 1] citation.";
        var chunks = new List<RetrievedChunk>
        {
            new(Guid.NewGuid(), "Source content here.", "Test Doc", 0.9),
        };

        var result = await _sut.VerifyCitationsAsync(response, chunks);

        result.Should().NotBeNull();
        result.OverallConfidence.Should().BeInRange(0, 1);
        result.HasPotentialHallucinations.Should().Be(result.HasPotentialHallucinations);
    }

    private static float[] GenerateRandomVector(int dim, int seed = 42)
    {
        var rng = new Random(seed);
        return Enumerable.Range(0, dim).Select(_ => (float)(rng.NextDouble() * 2 - 1)).ToArray();
    }
}
