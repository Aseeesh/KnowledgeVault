using KnowledgeVault.Core.Interfaces.Services;

namespace KnowledgeVault.Application.Services.Ingestion;

public class ChunkingService : IChunkingService
{
    public IReadOnlyList<TextChunk> ChunkText(
        string text, IReadOnlyList<DocumentSection>? sections = null,
        int chunkSize = 512, int overlap = 50)
    {
        if (sections is not null && sections.Count > 0)
            return ChunkBySections(sections, chunkSize, overlap);

        return ChunkByFixedSize(text, chunkSize, overlap);
    }

    private static List<TextChunk> ChunkBySections(
        IReadOnlyList<DocumentSection> sections, int chunkSize, int overlap)
    {
        var chunks = new List<TextChunk>();
        int index = 0;

        foreach (var section in sections)
        {
            var sectionChunks = ChunkByFixedSize(section.Content, chunkSize, overlap);
            foreach (var chunk in sectionChunks)
            {
                chunks.Add(chunk with { Index = index, SectionHeading = section.Heading });
                index++;
            }
        }

        return chunks;
    }

    private static List<TextChunk> ChunkByFixedSize(string text, int chunkSize, int overlap)
    {
        var chunks = new List<TextChunk>();
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (words.Length == 0)
            return chunks;

        int index = 0;
        int pos = 0;

        while (pos < words.Length)
        {
            var end = Math.Min(pos + chunkSize, words.Length);
            var chunkWords = words[pos..end];
            var content = string.Join(' ', chunkWords);

            chunks.Add(new TextChunk(content, index, null, chunkWords.Length));

            pos += chunkSize - overlap;
            if (pos >= words.Length) break;
            index++;
        }

        return chunks;
    }
}
