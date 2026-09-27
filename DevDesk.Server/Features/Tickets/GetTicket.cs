using Dapper;
using Microsoft.AspNetCore.Http.HttpResults;
using Npgsql;

namespace DevDesk.Server.Features.Tickets;

public static class GetTicketEndpoint
{
    public static void MapGetTicketEndpoint(this IEndpointRouteBuilder app) =>
        app.MapGet("/{id:guid}", Handle)
            .WithName("GetTicket");

    private static async Task<Results<Ok<TicketResponse>, NotFound>> Handle(
        Guid id,
        NpgsqlDataSource dataSource,
        CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);

        const string selectSql = """
            select id as Id, subject as Subject, description as Description,
                   requester_id as RequesterId, created_at as CreatedAt, status as Status,
                   assigned_agent as AssignedAgent, assigned_at as AssignedAt,
                   resolution_note as ResolutionNote, resolved_at as ResolvedAt, closed_at as ClosedAt
            from tickets
            where id = @Id
            """;

        var row = await connection.QuerySingleOrDefaultAsync<DbTicketRow>(
            new CommandDefinition(selectSql, new { Id = id }, cancellationToken: ct));

        return row is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(row.ToResponse());
    }
}

// Property-only (no primary constructor) so Dapper materializes via its parameterless-ctor +
// property-setter path. Npgsql returns timestamptz as DateTime, not DateTimeOffset, hence DateTime here.
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

    public TicketResponse ToResponse() => new(
        Id, Subject, Description, RequesterId, new DateTimeOffset(CreatedAt), Status,
        AssignedAgent,
        AssignedAt is { } assignedAt ? new DateTimeOffset(assignedAt) : null,
        ResolutionNote,
        ResolvedAt is { } resolvedAt ? new DateTimeOffset(resolvedAt) : null,
        ClosedAt is { } closedAt ? new DateTimeOffset(closedAt) : null);
}
