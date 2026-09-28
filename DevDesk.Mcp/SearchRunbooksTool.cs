using System.ComponentModel;
using System.Globalization;
using Dapper;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using Npgsql;

namespace DevDesk.Mcp;

[McpServerToolType]
public static class SearchRunbooksTool
{
    [
        McpServerTool(Name = ToolNames.SearchRunbooks),
        Description(
            "Search the runbook knowledge base for passages relevant to a query, using vector similarity over ingested runbook chunks."
        )
    ]
    public static async Task<string> SearchRunbooks(
        [Description("What to search for, e.g. \"how to reset a customer's 2FA\"")] string query,
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        NpgsqlDataSource dataSource,
        IOptions<SearchOptions> options,
        CancellationToken cancellationToken
    )
    {
        var embeddings = await embeddingGenerator.GenerateAsync(
            [query],
            cancellationToken: cancellationToken
        );
        var vectorLiteral = ToVectorLiteral(embeddings[0].Vector.Span);

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        const string searchSql = """
            select
                source_path as SourcePath,
                content as Content,
                1 - (embedding <=> @QueryEmbedding::vector) as Score
            from doc_chunks
            order by embedding <=> @QueryEmbedding::vector
            limit @TopK
            """;

        var matches = (
            await connection.QueryAsync<DbRunbookMatch>(
                new CommandDefinition(
                    searchSql,
                    new { QueryEmbedding = vectorLiteral, TopK = options.Value.RunbookResults },
                    cancellationToken: cancellationToken
                )
            )
        ).ToList();

        return matches.Count == 0
            ? "No matching runbook passages found."
            : string.Join(
                "\n\n",
                matches.Select(m => $"[{m.SourcePath}] (score {m.Score:F2})\n{m.Content}")
            );
    }

    private static string ToVectorLiteral(ReadOnlySpan<float> vector)
    {
        var values = new string[vector.Length];
        for (var i = 0; i < vector.Length; i++)
        {
            values[i] = vector[i].ToString(CultureInfo.InvariantCulture);
        }

        return $"[{string.Join(',', values)}]";
    }
}

file sealed record DbRunbookMatch
{
    public string SourcePath { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public double Score { get; init; }
}
