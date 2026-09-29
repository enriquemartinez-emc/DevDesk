using System.Globalization;
using Azure.Storage.Blobs;
using Dapper;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Npgsql;

namespace DevDesk.Ingestion;

public sealed class RunbookIngestor(
    BlobServiceClient blobService,
    NpgsqlDataSource dataSource,
    IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
    IOptions<IngestionOptions> options,
    ILogger<RunbookIngestor> logger
)
{
    private const string RunbookFilePattern = "*.md";

    private const string DeleteChunksSql = "delete from doc_chunks where source_path = @SourcePath";

    private const string InsertChunkSql = """
        insert into doc_chunks (
            id,
            source_path,
            chunk_index,
            content,
            embedding
        )
        values (
            @Id,
            @SourcePath,
            @ChunkIndex,
            @Content,
            @Embedding::vector
        )
        """;

    private readonly IngestionOptions settings = options.Value;

    public async Task RunAsync(CancellationToken ct)
    {
        var container = blobService.GetBlobContainerClient(settings.BlobContainer);
        await container.CreateIfNotExistsAsync(cancellationToken: ct);

        await UploadRunbooksAsync(container, ct);

        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await foreach (var blob in container.GetBlobsAsync(cancellationToken: ct))
        {
            await IngestAsync(container, connection, blob.Name, ct);
        }
    }

    private async Task UploadRunbooksAsync(BlobContainerClient container, CancellationToken ct)
    {
        foreach (var path in Directory.EnumerateFiles(settings.RunbooksPath, RunbookFilePattern))
        {
            var blobName = Path.GetFileName(path);
            await container.GetBlobClient(blobName).UploadAsync(path, overwrite: true, ct);
            logger.LogInformation("Uploaded {Blob} to blob storage", blobName);
        }
    }

    private async Task IngestAsync(
        BlobContainerClient container,
        NpgsqlConnection connection,
        string blobName,
        CancellationToken ct
    )
    {
        var download = await container.GetBlobClient(blobName).DownloadContentAsync(ct);
        var chunks = RunbookChunker.Chunk(
            download.Value.Content.ToString(),
            settings.MaxChunkCharacters
        );
        var embeddings = await embeddingGenerator.GenerateAsync(chunks, cancellationToken: ct);

        await using var transaction = await connection.BeginTransactionAsync(ct);
        await connection.ExecuteAsync(
            new CommandDefinition(
                DeleteChunksSql,
                new { SourcePath = blobName },
                transaction,
                cancellationToken: ct
            )
        );

        for (var index = 0; index < chunks.Count; index++)
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    InsertChunkSql,
                    new
                    {
                        Id = Guid.NewGuid(),
                        SourcePath = blobName,
                        ChunkIndex = index,
                        Content = chunks[index],
                        Embedding = ToVectorLiteral(embeddings[index].Vector.Span),
                    },
                    transaction,
                    cancellationToken: ct
                )
            );
        }

        await transaction.CommitAsync(ct);
        logger.LogInformation("Ingested {Blob}: {Count} chunks", blobName, chunks.Count);
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
