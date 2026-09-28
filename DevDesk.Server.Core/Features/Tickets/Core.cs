namespace DevDesk.Server.Core.Features.Tickets;

// ---- Value objects (Rule 7): wrap ids so they can't be swapped for each other ----
public readonly record struct TicketId(Guid Value);

public readonly record struct RequesterId(Guid Value);

public readonly record struct AgentId(Guid Value);

// ---- OR: the ticket lifecycle — closed hierarchy (Rule 3) ----
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

// ---- Transitions (Rule 4, Rule 5: new() across different types) ----
public static class TicketRules
{
    public static OpenTicket Create(
        TicketId id,
        string subject,
        string description,
        RequesterId requester,
        DateTimeOffset createdAt
    ) => new(id, subject, description, requester, createdAt);

    public static InProgressTicket Assign(
        OpenTicket ticket,
        AgentId agent,
        DateTimeOffset assignedAt
    ) =>
        new(
            ticket.Id,
            ticket.Subject,
            ticket.Description,
            ticket.Requester,
            ticket.CreatedAt,
            agent,
            assignedAt
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

    public static InProgressTicket Reopen(ClosedTicket ticket, DateTimeOffset reopenedAt) =>
        new(
            ticket.Id,
            ticket.Subject,
            ticket.Description,
            ticket.Requester,
            ticket.CreatedAt,
            ticket.AssignedAgent,
            reopenedAt
        );
}
