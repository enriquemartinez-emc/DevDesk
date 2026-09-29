using System.ComponentModel;
using System.Text.RegularExpressions;
using Dapper;
using ModelContextProtocol.Server;
using Npgsql;

namespace DevDesk.Mcp;

[McpServerToolType]
public static partial class LookupTicketTool
{
    [
        McpServerTool(Name = "lookup_ticket"),
        Description(
            "Look up a support ticket by its number and return its current status, assignment, and resolution details."
        )
    ]
    public static async Task<string> LookupTicket(
        [Description("The ticket's number, e.g. \"1042\" or \"#1042\"")] string ticketNumber,
        NpgsqlDataSource dataSource,
        CancellationToken cancellationToken
    )
    {
        // The model may pass the number with a "#" or other wording around it; keep only the digits.
        var digits = DigitsOnly().Match(ticketNumber);
        if (!digits.Success || !long.TryParse(digits.Value, out var number))
        {
            return $"'{ticketNumber}' isn't a ticket number.";
        }

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        const string lookupSql =
            $"select {TicketRow.Columns} from tickets where ticket_number = @Number";

        var ticket = await connection.QuerySingleOrDefaultAsync<TicketRow>(
            new CommandDefinition(
                lookupSql,
                new { Number = number },
                cancellationToken: cancellationToken
            )
        );

        if (ticket is null)
        {
            return $"No ticket found with number {number}.";
        }

        return ticket.Status switch
        {
            "open" =>
                $"Ticket #{ticket.Number} ('{ticket.Subject}') is open and not yet assigned. Created {ticket.CreatedAt:u}.",
            "in_progress" =>
                $"Ticket #{ticket.Number} ('{ticket.Subject}') is in progress, assigned to agent {ticket.AssignedAgent} since {ticket.AssignedAt:u}.",
            "resolved" =>
                $"Ticket #{ticket.Number} ('{ticket.Subject}') was resolved at {ticket.ResolvedAt:u} by agent {ticket.AssignedAgent}. Resolution: {ticket.ResolutionNote}",
            "closed" =>
                $"Ticket #{ticket.Number} ('{ticket.Subject}') is closed (closed at {ticket.ClosedAt:u}). Resolution: {ticket.ResolutionNote}",
            _ => $"Ticket #{ticket.Number} has an unrecognized status '{ticket.Status}'.",
        };
    }

    [GeneratedRegex(@"\d+")]
    private static partial Regex DigitsOnly();
}
