using System.Diagnostics;
using DevDesk.Server.Core.Features.Tickets;

namespace DevDesk.Server.Features.Tickets;

public sealed record TicketResponse(
    Guid Id,
    string Subject,
    string Description,
    Guid RequesterId,
    DateTimeOffset CreatedAt,
    string Status,
    Guid? AssignedAgent,
    DateTimeOffset? AssignedAt,
    string? ResolutionNote,
    DateTimeOffset? ResolvedAt,
    DateTimeOffset? ClosedAt
);

public static class TicketResponses
{
    public static TicketResponse FromDomain(Ticket ticket) =>
        ticket switch
        {
            OpenTicket t => new TicketResponse(
                t.Id.Value,
                t.Subject,
                t.Description,
                t.Requester.Value,
                t.CreatedAt,
                "open",
                null,
                null,
                null,
                null,
                null
            ),
            InProgressTicket t => new TicketResponse(
                t.Id.Value,
                t.Subject,
                t.Description,
                t.Requester.Value,
                t.CreatedAt,
                "in_progress",
                t.AssignedAgent.Value,
                t.AssignedAt,
                null,
                null,
                null
            ),
            ResolvedTicket t => new TicketResponse(
                t.Id.Value,
                t.Subject,
                t.Description,
                t.Requester.Value,
                t.CreatedAt,
                "resolved",
                t.AssignedAgent.Value,
                t.AssignedAt,
                t.ResolutionNote,
                t.ResolvedAt,
                null
            ),
            ClosedTicket t => new TicketResponse(
                t.Id.Value,
                t.Subject,
                t.Description,
                t.Requester.Value,
                t.CreatedAt,
                "closed",
                t.AssignedAgent.Value,
                t.AssignedAt,
                t.ResolutionNote,
                t.ResolvedAt,
                t.ClosedAt
            ),
            _ => throw new UnreachableException($"Unhandled ticket case: {ticket.GetType()}"),
        };
}
