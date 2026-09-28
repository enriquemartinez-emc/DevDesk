using Dapper;
using Microsoft.AspNetCore.Http.HttpResults;
using Npgsql;

namespace DevDesk.Server.Features.Tickets;

public static class ListTicketsEndpoint
{
    // Simple recency cap for now; revisit with real paging once a client needs to page through history.
    private const int MaxResults = 100;

    public static void MapListTicketsEndpoint(this IEndpointRouteBuilder app) =>
        app.MapGet("/", Handle).WithName("ListTickets");

    private static async Task<Ok<IReadOnlyList<TicketResponse>>> Handle(
        NpgsqlDataSource dataSource,
        CancellationToken ct
    )
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);

        const string selectSql = """
            select
                id as Id,
                subject as Subject,
                description as Description,
                requester_id as RequesterId,
                created_at as CreatedAt,
                status as Status,
                assigned_agent as AssignedAgent,
                assigned_at as AssignedAt,
                resolution_note as ResolutionNote,
                resolved_at as ResolvedAt,
                closed_at as ClosedAt
            from tickets
            order by created_at desc
            limit @MaxResults
            """;

        var rows = await connection.QueryAsync<DbTicketRow>(
            new CommandDefinition(selectSql, new { MaxResults }, cancellationToken: ct)
        );

        var response = rows.Select(row => row.ToResponse()).ToList();
        return TypedResults.Ok<IReadOnlyList<TicketResponse>>(response);
    }
}

// Property-only so Dapper can materialize it; Npgsql returns timestamptz as DateTime.
file sealed record DbTicketRow
{
    public Guid Id { get; init; }
    public string Subject { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public Guid RequesterId { get; init; }
    public DateTime CreatedAt { get; init; }
    public string Status { get; init; } = string.Empty;
    public Guid? AssignedAgent { get; init; }
    public DateTime? AssignedAt { get; init; }
    public string? ResolutionNote { get; init; }
    public DateTime? ResolvedAt { get; init; }
    public DateTime? ClosedAt { get; init; }

    public TicketResponse ToResponse() =>
        new(
            Id,
            Subject,
            Description,
            RequesterId,
            new DateTimeOffset(CreatedAt),
            Status,
            AssignedAgent,
            AssignedAt is { } assignedAt ? new DateTimeOffset(assignedAt) : null,
            ResolutionNote,
            ResolvedAt is { } resolvedAt ? new DateTimeOffset(resolvedAt) : null,
            ClosedAt is { } closedAt ? new DateTimeOffset(closedAt) : null
        );
}
