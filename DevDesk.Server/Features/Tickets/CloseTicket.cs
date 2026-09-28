using Dapper;
using DevDesk.Server.Core.Features.Tickets;
using Microsoft.AspNetCore.Http.HttpResults;
using Npgsql;

namespace DevDesk.Server.Features.Tickets;

public static class CloseTicketEndpoint
{
    public static void MapCloseTicketEndpoint(this IEndpointRouteBuilder app) =>
        app.MapPost("/{id:guid}/close", Handle).WithName("CloseTicket");

    private static async Task<Results<Ok<TicketResponse>, ProblemHttpResult>> Handle(
        Guid id,
        NpgsqlDataSource dataSource,
        TimeProvider time,
        CancellationToken ct
    )
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
            new CommandDefinition(selectSql, new { Id = id }, cancellationToken: ct)
        );

        if (row is null)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Ticket not found.",
                detail: $"Ticket '{id}' does not exist."
            );
        }

        if (row.ToDomain() is not ResolvedTicket resolved)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Ticket cannot be closed.",
                detail: $"Only resolved tickets can be closed, and this ticket is {row.Status}."
            );
        }

        var closed = TicketRules.Close(resolved, time.GetUtcNow());

        const string updateSql = """
            update tickets
            set status = @Status, closed_at = @ClosedAt
            where id = @Id
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                updateSql,
                new
                {
                    Id = closed.Id.Value,
                    Status = TicketStatuses.Closed,
                    closed.ClosedAt,
                },
                cancellationToken: ct
            )
        );

        return TypedResults.Ok(TicketResponses.FromDomain(closed));
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

    public Ticket ToDomain() =>
        Status switch
        {
            TicketStatuses.Open => new OpenTicket(
                new TicketId(Id),
                Subject,
                Description,
                new RequesterId(RequesterId),
                new DateTimeOffset(CreatedAt)
            ),
            TicketStatuses.InProgress => new InProgressTicket(
                new TicketId(Id),
                Subject,
                Description,
                new RequesterId(RequesterId),
                new DateTimeOffset(CreatedAt),
                new AgentId(AssignedAgent!.Value),
                new DateTimeOffset(AssignedAt!.Value)
            ),
            TicketStatuses.Resolved => new ResolvedTicket(
                new TicketId(Id),
                Subject,
                Description,
                new RequesterId(RequesterId),
                new DateTimeOffset(CreatedAt),
                new AgentId(AssignedAgent!.Value),
                new DateTimeOffset(AssignedAt!.Value),
                ResolutionNote!,
                new DateTimeOffset(ResolvedAt!.Value)
            ),
            TicketStatuses.Closed => new ClosedTicket(
                new TicketId(Id),
                Subject,
                Description,
                new RequesterId(RequesterId),
                new DateTimeOffset(CreatedAt),
                new AgentId(AssignedAgent!.Value),
                new DateTimeOffset(AssignedAt!.Value),
                ResolutionNote!,
                new DateTimeOffset(ResolvedAt!.Value),
                new DateTimeOffset(ClosedAt!.Value)
            ),
            _ => throw new InvalidOperationException($"Unsupported ticket status '{Status}'."),
        };
}
