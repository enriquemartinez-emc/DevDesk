namespace DevDesk.Mcp;

// Property-only so Dapper can materialize it; Npgsql returns timestamptz as DateTime.
internal sealed record TicketRow
{
    public const string Columns = """
        id as Id,
        ticket_number as Number,
        subject as Subject,
        description as Description,
        created_at as CreatedAt,
        status as Status,
        assigned_agent as AssignedAgent,
        assigned_at as AssignedAt,
        resolution_note as ResolutionNote,
        resolved_at as ResolvedAt,
        closed_at as ClosedAt
        """;

    public Guid Id { get; init; }
    public long Number { get; init; }
    public string Subject { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public string Status { get; init; } = string.Empty;
    public Guid? AssignedAgent { get; init; }
    public DateTime? AssignedAt { get; init; }
    public string? ResolutionNote { get; init; }
    public DateTime? ResolvedAt { get; init; }
    public DateTime? ClosedAt { get; init; }
}
