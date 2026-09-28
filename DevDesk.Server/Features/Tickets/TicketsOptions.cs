using System.ComponentModel.DataAnnotations;

namespace DevDesk.Server.Features.Tickets;

public sealed class TicketsOptions
{
    public const string SectionName = "Tickets";

    [Range(1, 500)]
    public int MaxListResults { get; init; }
}
