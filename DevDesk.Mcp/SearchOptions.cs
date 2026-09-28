using System.ComponentModel.DataAnnotations;

namespace DevDesk.Mcp;

public sealed class SearchOptions
{
    public const string SectionName = "Search";

    [Range(1, 20)]
    public int TicketResults { get; init; }

    [Range(1, 20)]
    public int RunbookResults { get; init; }
}
