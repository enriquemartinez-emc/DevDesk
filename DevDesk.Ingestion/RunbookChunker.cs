using System.Text;

namespace DevDesk.Ingestion;

public static class RunbookChunker
{
    public static IReadOnlyList<string> Chunk(string markdown, int maxChunkCharacters)
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
            foreach (var piece in SplitOnParagraphs(text, maxChunkCharacters))
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

    private static IEnumerable<string> SplitOnParagraphs(string text, int maxChunkCharacters)
    {
        var current = new StringBuilder();
        foreach (var paragraph in text.Split("\n\n", StringSplitOptions.RemoveEmptyEntries))
        {
            if (current.Length > 0 && current.Length + paragraph.Length > maxChunkCharacters)
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
