using Npgsql;

namespace DevDesk.Mcp.Features.Runbooks;

public static class RunbooksSchema
{
    // nomic-embed-text produces 768-dimension embeddings.
    public const int EmbeddingDimensions = 768;

    public static async Task EnsureCreatedAsync(NpgsqlDataSource dataSource, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);

        await using (var extensionCommand = new NpgsqlCommand("create extension if not exists vector", connection))
        {
            await extensionCommand.ExecuteNonQueryAsync(ct);
        }

        var createTableSql = $"""
            create table if not exists doc_chunks (
                id uuid primary key,
                source_path text not null,
                chunk_index int not null,
                content text not null,
                embedding vector({EmbeddingDimensions}) not null
            )
            """;

        await using var command = new NpgsqlCommand(createTableSql, connection);
        await command.ExecuteNonQueryAsync(ct);
    }
}
