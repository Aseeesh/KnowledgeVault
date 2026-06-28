using FluentAssertions;
using KnowledgeVault.Application.Services.Ingestion;
using KnowledgeVault.Core.Interfaces.Services;

namespace KnowledgeVault.Tests.Unit.Services;

public class ChunkingServiceTests
{
    private readonly ChunkingService _sut = new();

    [Fact]
    public void ChunkText_EmptyString_ReturnsEmpty()
    {
        var result = _sut.ChunkText("");
        result.Should().BeEmpty();
    }

    [Fact]
    public void ChunkText_ShortText_ReturnsSingleChunk()
    {
        var text = "This is a short text with a few words.";
        var result = _sut.ChunkText(text, chunkSize: 100);
        result.Should().HaveCount(1);
        result[0].Content.Should().Be(text);
        result[0].Index.Should().Be(0);
    }

    [Fact]
    public void ChunkText_LongText_ReturnsMultipleChunks()
    {
        var words = Enumerable.Range(1, 100).Select(i => $"word{i}");
        var text = string.Join(" ", words);

        var result = _sut.ChunkText(text, chunkSize: 20, overlap: 5);

        result.Should().HaveCountGreaterThan(1);
        result.All(c => c.TokenCount <= 20).Should().BeTrue();
    }

    [Fact]
    public void ChunkText_WithOverlap_ChunksShareWords()
    {
        var words = Enumerable.Range(1, 40).Select(i => $"word{i}");
        var text = string.Join(" ", words);

        var result = _sut.ChunkText(text, chunkSize: 20, overlap: 5);

        result.Should().HaveCountGreaterThan(1);
        var firstChunkWords = result[0].Content.Split(' ');
        var secondChunkWords = result[1].Content.Split(' ');
        firstChunkWords.Intersect(secondChunkWords).Should().NotBeEmpty();
    }

    [Fact]
    public void ChunkText_WithSections_ChunksPerSection()
    {
        var sections = new List<DocumentSection>
        {
            new("Introduction", "This is the introduction section with enough words to fill a chunk.", 1),
            new("Methods", "This is the methods section describing the approach used in the study.", 1),
        };

        var result = _sut.ChunkText("", sections, chunkSize: 100);

        result.Should().HaveCount(2);
        result[0].SectionHeading.Should().Be("Introduction");
        result[1].SectionHeading.Should().Be("Methods");
    }

    [Fact]
    public void ChunkText_IndexesAreSequential()
    {
        var words = Enumerable.Range(1, 60).Select(i => $"word{i}");
        var text = string.Join(" ", words);

        var result = _sut.ChunkText(text, chunkSize: 20, overlap: 5);

        for (int i = 0; i < result.Count; i++)
            result[i].Index.Should().Be(i);
    }

    [Fact]
    public void ChunkText_TokenCountIsAccurate()
    {
        var text = "one two three four five six seven eight nine ten";
        var result = _sut.ChunkText(text, chunkSize: 100);

        result[0].TokenCount.Should().Be(10);
    }
}
