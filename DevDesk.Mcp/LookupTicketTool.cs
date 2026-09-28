using System.ComponentModel;
using Dapper;
using ModelContextProtocol.Server;
using Npgsql;

namespace DevDesk.Mcp;

[McpServerToolType]
public static class LookupTicketTool
{
    [
        McpServerTool(Name = "lookup_ticket"),
        Description(
            "Look up a support ticket by id and return its current status, assignment, and resolution details."
        )
    ]
    public static async Task<string> LookupTicket(
        [Description("The ticket's id (GUID)")] Guid ticketId,
        NpgsqlDataSource dataSource,
        CancellationToken cancellationToken
    )
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        const string lookupSql = $"select {TicketRow.Columns} from tickets where id = @Id";

        var ticket = await connection.QuerySingleOrDefaultAsync<TicketRow>(
            new CommandDefinition(
                lookupSql,
                new { Id = ticketId },
                cancellationToken: cancellationToken
            )
        );

        if (ticket is null)
        {
            return $"No ticket found with id {ticketId}.";
        }

        return ticket.Status switch
        {
            "open" =>
                $"Ticket {ticket.Id} ('{ticket.Subject}') is open and not yet assigned. Created {ticket.CreatedAt:u}.",
            "in_progress" =>
                $"Ticket {ticket.Id} ('{ticket.Subject}') is in progress, assigned to agent {ticket.AssignedAgent} since {ticket.AssignedAt:u}.",
            "resolved" =>
                $"Ticket {ticket.Id} ('{ticket.Subject}') was resolved at {ticket.ResolvedAt:u} by agent {ticket.AssignedAgent}. Resolution: {ticket.ResolutionNote}",
            "closed" =>
                $"Ticket {ticket.Id} ('{ticket.Subject}') is closed (closed at {ticket.ClosedAt:u}). Resolution: {ticket.ResolutionNote}",
            _ => $"Ticket {ticket.Id} has an unrecognized status '{ticket.Status}'.",
        };
    }
}
