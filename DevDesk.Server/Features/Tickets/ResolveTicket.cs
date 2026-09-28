using Dapper;
using DevDesk.Server.Common;
using DevDesk.Server.Core.Features.Tickets;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Npgsql;

namespace DevDesk.Server.Features.Tickets;

public sealed record ResolveTicketRequest(string ResolutionNote);

public class ResolveTicketValidator : AbstractValidator<ResolveTicketRequest>
{
    public ResolveTicketValidator()
    {
        RuleFor(x => x.ResolutionNote).NotEmpty();
    }
}

public static class ResolveTicketEndpoint
{
    public static void MapResolveTicketEndpoint(this IEndpointRouteBuilder app) =>
        app.MapPost("/{id:guid}/resolve", Handle)
            .WithValidation<ResolveTicketRequest>()
            .WithName("ResolveTicket");

    private static async Task<Results<Ok<TicketResponse>, ProblemHttpResult>> Handle(
        Guid id,
        ResolveTicketRequest request,
        NpgsqlDataSource dataSource,
        TimeProvider time,
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

        if (row.ToDomain() is not InProgressTicket inProgress)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Ticket cannot be resolved.",
                detail: $"Only in-progress tickets can be resolved, and this ticket is {row.Status}."
            );
        }

        var resolved = TicketRules.Resolve(inProgress, request.ResolutionNote, time.GetUtcNow());

        const string updateSql = """
            update tickets
            set
                status = @Status,
                resolution_note = @ResolutionNote,
                resolved_at = @ResolvedAt
            where id = @Id
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                updateSql,
                new
                {
                    Id = resolved.Id.Value,
                    Status = TicketStatuses.Resolved,
                    resolved.ResolutionNote,
                    resolved.ResolvedAt,
                },
                cancellationToken: ct
            )
        );

        return TypedResults.Ok(TicketResponses.FromDomain(resolved));
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
