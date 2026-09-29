using System.Diagnostics;
using DevDesk.Server.Core.Features.Tickets;

namespace DevDesk.Server.Features.Tickets;

public sealed record TicketResponse(
    Guid Id,
    long Number,
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
    public static TicketResponse FromDomain(Ticket ticket, long number) =>
        ticket switch
        {
            OpenTicket t => new TicketResponse(
                t.Id.Value,
                number,
                t.Subject,
                t.Description,
                t.Requester.Value,
                t.CreatedAt,
                TicketStatuses.Open,
                null,
                null,
                null,
                null,
                null
            ),
            InProgressTicket t => new TicketResponse(
                t.Id.Value,
                number,
                t.Subject,
                t.Description,
                t.Requester.Value,
                t.CreatedAt,
                TicketStatuses.InProgress,
                t.AssignedAgent.Value,
                t.AssignedAt,
                null,
                null,
                null
            ),
            ResolvedTicket t => new TicketResponse(
                t.Id.Value,
                number,
                t.Subject,
                t.Description,
                t.Requester.Value,
                t.CreatedAt,
                TicketStatuses.Resolved,
                t.AssignedAgent.Value,
                t.AssignedAt,
                t.ResolutionNote,
                t.ResolvedAt,
                null
            ),
            ClosedTicket t => new TicketResponse(
                t.Id.Value,
                number,
                t.Subject,
                t.Description,
                t.Requester.Value,
                t.CreatedAt,
                TicketStatuses.Closed,
                t.AssignedAgent.Value,
                t.AssignedAt,
                t.ResolutionNote,
                t.ResolvedAt,
                t.ClosedAt
            ),
            _ => throw new UnreachableException($"Unhandled ticket case: {ticket.GetType()}"),
        };
}
