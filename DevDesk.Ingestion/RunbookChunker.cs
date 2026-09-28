using System.Text;

namespace DevDesk.Ingestion;

public static class RunbookChunker
{
    private const int MaxChunkCharacters = 1200;

    // One chunk per "## " section, prefixed with the document title so a chunk stands on its own.
    // Sections longer than the limit are split on paragraph boundaries.
    public static IReadOnlyList<string> Chunk(string markdown)
    {
        var title = string.Empty;
        var heading = string.Empty;
        var body = new StringBuilder();
        var chunks = new List<string>();

        void Flush()
        {
            var text = body.ToString().Trim();
            body.Clear();
            if (text.Length == 0)
            {
                return;
            }

            var prefix = heading.Length == 0 ? title : $"{title} - {heading}";
            foreach (var piece in SplitOnParagraphs(text))
            {
                chunks.Add($"{prefix}\n{piece}");
            }
        }

        foreach (var line in markdown.ReplaceLineEndings("\n").Split('\n'))
        {
            if (line.StartsWith("## ", StringComparison.Ordinal))
            {
                Flush();
                heading = line[3..].Trim();
            }
            else if (line.StartsWith("# ", StringComparison.Ordinal))
            {
                title = line[2..].Trim();
            }
            else
            {
                body.AppendLine(line);
            }
        }

        Flush();
        return chunks;
    }

    private static IEnumerable<string> SplitOnParagraphs(string text)
    {
        var current = new StringBuilder();
        foreach (var paragraph in text.Split("\n\n", StringSplitOptions.RemoveEmptyEntries))
        {
            if (current.Length > 0 && current.Length + paragraph.Length > MaxChunkCharacters)
            {
                yield return current.ToString().Trim();
                current.Clear();
            }

            current.Append(paragraph).Append("\n\n");
        }

        if (current.Length > 0)
        {
            yield return current.ToString().Trim();
        }
    }
}
