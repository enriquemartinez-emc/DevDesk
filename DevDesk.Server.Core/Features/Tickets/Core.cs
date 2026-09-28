namespace DevDesk.Server.Core.Features.Tickets;

// ---- Value objects: ids that can't be swapped for each other ----
public readonly record struct TicketId(Guid Value);

public readonly record struct RequesterId(Guid Value);

public readonly record struct AgentId(Guid Value);

// ---- Ticket lifecycle: a closed hierarchy ----
public abstract record Ticket
{
    private protected Ticket(
        TicketId id,
        string subject,
        string description,
        RequesterId requester,
        DateTimeOffset createdAt
    )
    {
        Id = id;
        Subject = subject;
        Description = description;
        Requester = requester;
        CreatedAt = createdAt;
    }

    public TicketId Id { get; }
    public string Subject { get; }
    public string Description { get; }
    public RequesterId Requester { get; }
    public DateTimeOffset CreatedAt { get; }
}

public sealed record OpenTicket(
    TicketId Id,
    string Subject,
    string Description,
    RequesterId Requester,
    DateTimeOffset CreatedAt
) : Ticket(Id, Subject, Description, Requester, CreatedAt);

public sealed record InProgressTicket(
    TicketId Id,
    string Subject,
    string Description,
    RequesterId Requester,
    DateTimeOffset CreatedAt,
    AgentId AssignedAgent,
    DateTimeOffset AssignedAt
) : Ticket(Id, Subject, Description, Requester, CreatedAt);

public sealed record ResolvedTicket(
    TicketId Id,
    string Subject,
    string Description,
    RequesterId Requester,
    DateTimeOffset CreatedAt,
    AgentId AssignedAgent,
    DateTimeOffset AssignedAt,
    string ResolutionNote,
    DateTimeOffset ResolvedAt
) : Ticket(Id, Subject, Description, Requester, CreatedAt);

public sealed record ClosedTicket(
    TicketId Id,
    string Subject,
    string Description,
    RequesterId Requester,
    DateTimeOffset CreatedAt,
    AgentId AssignedAgent,
    DateTimeOffset AssignedAt,
    string ResolutionNote,
    DateTimeOffset ResolvedAt,
    DateTimeOffset ClosedAt
) : Ticket(Id, Subject, Description, Requester, CreatedAt);

// ---- Outcomes: only where a rule depends on more than the ticket's state ----
public abstract record AssignOutcome
{
    private protected AssignOutcome() { }
}

public sealed record Assigned(InProgressTicket Ticket) : AssignOutcome;

public sealed record AgentIsRequester : AssignOutcome;

public abstract record ReopenOutcome
{
    private protected ReopenOutcome() { }
}

public sealed record Reopened(InProgressTicket Ticket) : ReopenOutcome;

public sealed record ReopenWindowElapsed(DateTimeOffset ExpiredAt) : ReopenOutcome;

// ---- Transitions: each takes exactly the state it applies to, so illegal calls don't compile ----
public static class TicketRules
{
    // A closed ticket can still be reopened for this long; after that the customer opens a new one.
    public static readonly TimeSpan ReopenWindow = TimeSpan.FromDays(14);

    public static OpenTicket Create(
        TicketId id,
        string subject,
        string description,
        RequesterId requester,
        DateTimeOffset createdAt
    ) => new(id, subject, description, requester, createdAt);

    // Nobody works their own request.
    public static AssignOutcome Assign(
        OpenTicket ticket,
        AgentId agent,
        DateTimeOffset assignedAt
    ) =>
        ticket.Requester.Value == agent.Value
            ? new AgentIsRequester()
            : new Assigned(
                new InProgressTicket(
                    ticket.Id,
                    ticket.Subject,
                    ticket.Description,
                    ticket.Requester,
                    ticket.CreatedAt,
                    agent,
                    assignedAt
                )
            );

    public static ResolvedTicket Resolve(
        InProgressTicket ticket,
        string resolutionNote,
        DateTimeOffset resolvedAt
    ) =>
        new(
            ticket.Id,
            ticket.Subject,
            ticket.Description,
            ticket.Requester,
            ticket.CreatedAt,
            ticket.AssignedAgent,
            ticket.AssignedAt,
            resolutionNote,
            resolvedAt
        );

    public static ClosedTicket Close(ResolvedTicket ticket, DateTimeOffset closedAt) =>
        new(
            ticket.Id,
            ticket.Subject,
            ticket.Description,
            ticket.Requester,
            ticket.CreatedAt,
            ticket.AssignedAgent,
            ticket.AssignedAt,
            ticket.ResolutionNote,
            ticket.ResolvedAt,
            closedAt
        );

    // Drops the resolution on reopen: it didn't hold, so it must not be recommended from ticket history again.
    public static InProgressTicket Reopen(ResolvedTicket ticket, DateTimeOffset reopenedAt) =>
        new(
            ticket.Id,
            ticket.Subject,
            ticket.Description,
            ticket.Requester,
            ticket.CreatedAt,
            ticket.AssignedAgent,
            reopenedAt
        );

    // A resolved ticket can always come back; a closed one only within ReopenWindow of closing.
    public static ReopenOutcome Reopen(ClosedTicket ticket, DateTimeOffset reopenedAt) =>
        reopenedAt - ticket.ClosedAt > ReopenWindow
            ? new ReopenWindowElapsed(ticket.ClosedAt + ReopenWindow)
            : new Reopened(
                new InProgressTicket(
                    ticket.Id,
                    ticket.Subject,
                    ticket.Description,
                    ticket.Requester,
                    ticket.CreatedAt,
                    ticket.AssignedAgent,
                    reopenedAt
                )
            );
}
