using System.Globalization;
using Azure.Storage.Blobs;
using Dapper;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace DevDesk.Ingestion;

public sealed class RunbookIngestor(
    BlobServiceClient blobService,
    NpgsqlDataSource dataSource,
    IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
    ILogger<RunbookIngestor> logger
)
{
    private const string ContainerName = "runbooks";

    public async Task RunAsync(string docsDirectory, CancellationToken ct)
    {
        var container = blobService.GetBlobContainerClient(ContainerName);
        await container.CreateIfNotExistsAsync(cancellationToken: ct);

        foreach (var path in Directory.EnumerateFiles(docsDirectory, "*.md"))
        {
            var blobName = Path.GetFileName(path);
            await container.GetBlobClient(blobName).UploadAsync(path, overwrite: true, ct);
            logger.LogInformation("Uploaded {Blob} to blob storage", blobName);
        }

        await using var connection = await dataSource.OpenConnectionAsync(ct);

        await foreach (var blob in container.GetBlobsAsync(cancellationToken: ct))
        {
            var content = (
                await container.GetBlobClient(blob.Name).DownloadContentAsync(ct)
            ).Value.Content.ToString();
            var chunks = RunbookChunker.Chunk(content);
            var embeddings = await embeddingGenerator.GenerateAsync(chunks, cancellationToken: ct);

            await using var transaction = await connection.BeginTransactionAsync(ct);

            await connection.ExecuteAsync(
                new CommandDefinition(
                    "delete from doc_chunks where source_path = @SourcePath",
                    new { SourcePath = blob.Name },
                    transaction,
                    cancellationToken: ct
                )
            );

            for (var i = 0; i < chunks.Count; i++)
            {
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        """
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
                        """,
                        new
                        {
                            Id = Guid.NewGuid(),
                            SourcePath = blob.Name,
                            ChunkIndex = i,
                            Content = chunks[i],
                            Embedding = ToVectorLiteral(embeddings[i].Vector.Span),
                        },
                        transaction,
                        cancellationToken: ct
                    )
                );
            }

            await transaction.CommitAsync(ct);
            logger.LogInformation("Ingested {Blob}: {Count} chunks", blob.Name, chunks.Count);
        }
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
