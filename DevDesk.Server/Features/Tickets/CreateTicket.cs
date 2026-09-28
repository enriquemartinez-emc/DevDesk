using Dapper;
using DevDesk.Server.Common;
using DevDesk.Server.Core.Features.Tickets;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Npgsql;

namespace DevDesk.Server.Features.Tickets;

public sealed record CreateTicketRequest(string Subject, string Description, Guid RequesterId);

public class CreateTicketValidator : AbstractValidator<CreateTicketRequest>
{
    public CreateTicketValidator()
    {
        RuleFor(x => x.Subject).NotEmpty();
        RuleFor(x => x.Description).NotEmpty();
        RuleFor(x => x.RequesterId).NotEmpty();
    }
}

public static class CreateTicketEndpoint
{
    public static void MapCreateTicketEndpoint(this IEndpointRouteBuilder app) =>
        app.MapPost("/", Handle).WithValidation<CreateTicketRequest>().WithName("CreateTicket");

    private static async Task<Results<CreatedAtRoute<TicketResponse>, ProblemHttpResult>> Handle(
        CreateTicketRequest request,
        NpgsqlDataSource dataSource,
        TimeProvider time,
        CancellationToken ct
    )
    {
        var ticket = TicketRules.Create(
            new TicketId(Guid.NewGuid()),
            request.Subject,
            request.Description,
            new RequesterId(request.RequesterId),
            time.GetUtcNow()
        );

        await using var connection = await dataSource.OpenConnectionAsync(ct);

        const string insertSql = """
            insert into tickets (id, subject, description, requester_id, created_at, status)
            values (@Id, @Subject, @Description, @RequesterId, @CreatedAt, @Status)
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                insertSql,
                new
                {
                    Id = ticket.Id.Value,
                    ticket.Subject,
                    ticket.Description,
                    RequesterId = ticket.Requester.Value,
                    ticket.CreatedAt,
                    Status = "open",
                },
                cancellationToken: ct
            )
        );

        var response = TicketResponses.FromDomain(ticket);
        return TypedResults.CreatedAtRoute(response, "GetTicket", new { id = response.Id });
    }
}
