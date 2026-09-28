using System.ComponentModel.DataAnnotations;

namespace DevDesk.Ingestion;

public sealed class IngestionOptions
{
    public const string SectionName = "Ingestion";

    [Required]
    public string DocsPath { get; init; } = string.Empty;

    [Required]
    public string BlobContainer { get; init; } = string.Empty;

    [Range(200, 8000)]
    public int MaxChunkCharacters { get; init; }
}
