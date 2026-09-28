namespace DevDesk.Server.Features.Tickets;

// The status values stored in tickets.status and returned in TicketResponse.Status.
public static class TicketStatuses
{
    public const string Open = "open";
    public const string InProgress = "in_progress";
    public const string Resolved = "resolved";
    public const string Closed = "closed";
}
