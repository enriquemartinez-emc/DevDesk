using System.ComponentModel;
using DevDesk.Mcp.Clients;
using ModelContextProtocol.Server;

namespace DevDesk.Mcp.Features.Tickets;

[McpServerToolType]
public static class LookupTicketTool
{
    [McpServerTool, Description("Look up a support ticket by id and return its current status, assignment, and resolution details.")]
    public static async Task<string> LookupTicket(
        [Description("The ticket's id (GUID)")] Guid ticketId,
        TicketsApiClient client,
        CancellationToken cancellationToken)
    {
        var ticket = await client.GetTicketAsync(ticketId, cancellationToken);
        if (ticket is null)
        {
            return $"No ticket found with id {ticketId}.";
        }

        return ticket.Status switch
        {
            "open" => $"Ticket {ticket.Id} ('{ticket.Subject}') is open and not yet assigned. Created {ticket.CreatedAt:u}.",
            "in_progress" => $"Ticket {ticket.Id} ('{ticket.Subject}') is in progress, assigned to agent {ticket.AssignedAgent} since {ticket.AssignedAt:u}.",
            "resolved" => $"Ticket {ticket.Id} ('{ticket.Subject}') was resolved at {ticket.ResolvedAt:u} by agent {ticket.AssignedAgent}. Resolution: {ticket.ResolutionNote}",
            "closed" => $"Ticket {ticket.Id} ('{ticket.Subject}') is closed (closed at {ticket.ClosedAt:u}). Resolution: {ticket.ResolutionNote}",
            _ => $"Ticket {ticket.Id} has an unrecognized status '{ticket.Status}'.",
        };
    }
}
