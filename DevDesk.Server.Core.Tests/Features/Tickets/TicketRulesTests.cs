using DevDesk.Server.Core.Features.Tickets;

namespace DevDesk.Server.Core.Tests.Features.Tickets;

public class TicketRulesTests
{
    private static readonly TicketId Id = new(Guid.NewGuid());
    private static readonly RequesterId Requester = new(Guid.NewGuid());
    private static readonly AgentId Agent = new(Guid.NewGuid());
    private static readonly DateTimeOffset Now = new(2026, 9, 27, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_returns_an_open_ticket_with_no_agent_assigned()
    {
        var ticket = TicketRules.Create(Id, "Can't log in", "2FA reset needed", Requester, Now);

        Assert.Equal(Id, ticket.Id);
        Assert.Equal(Requester, ticket.Requester);
        Assert.Equal(Now, ticket.CreatedAt);
    }

    [Fact]
    public void Assign_carries_forward_the_original_ticket_fields()
    {
        var open = TicketRules.Create(Id, "Can't log in", "2FA reset needed", Requester, Now);

        var inProgress = TicketRules.Assign(open, Agent, Now.AddMinutes(5));

        Assert.Equal(open.Id, inProgress.Id);
        Assert.Equal(open.Subject, inProgress.Subject);
        Assert.Equal(Agent, inProgress.AssignedAgent);
    }

    [Fact]
    public void Resolve_then_close_carries_forward_the_resolution()
    {
        var open = TicketRules.Create(Id, "Can't log in", "2FA reset needed", Requester, Now);
        var inProgress = TicketRules.Assign(open, Agent, Now.AddMinutes(5));

        var resolved = TicketRules.Resolve(
            inProgress,
            "Reset via admin console",
            Now.AddMinutes(10)
        );
        var closed = TicketRules.Close(resolved, Now.AddMinutes(15));

        Assert.Equal("Reset via admin console", closed.ResolutionNote);
        Assert.Equal(resolved.ResolvedAt, closed.ResolvedAt);
        Assert.Equal(Agent, closed.AssignedAgent);
    }

    [Fact]
    public void Reopen_from_closed_goes_back_to_in_progress_with_the_same_agent()
    {
        var open = TicketRules.Create(Id, "Can't log in", "2FA reset needed", Requester, Now);
        var inProgress = TicketRules.Assign(open, Agent, Now.AddMinutes(5));
        var resolved = TicketRules.Resolve(
            inProgress,
            "Reset via admin console",
            Now.AddMinutes(10)
        );
        var closed = TicketRules.Close(resolved, Now.AddMinutes(15));

        var reopened = TicketRules.Reopen(closed, Now.AddDays(1));

        Assert.IsType<InProgressTicket>(reopened);
        Assert.Equal(Agent, reopened.AssignedAgent);
        Assert.Equal(Now.AddDays(1), reopened.AssignedAt);
    }
}
