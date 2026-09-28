using DevDesk.Server.Core.Features.Tickets;

namespace DevDesk.Server.Core.Tests.Features.Tickets;

public class TicketRulesTests
{
    private static readonly TicketId Id = new(Guid.NewGuid());
    private static readonly RequesterId Requester = new(Guid.NewGuid());
    private static readonly AgentId Agent = new(Guid.NewGuid());
    private static readonly DateTimeOffset Now = new(2026, 9, 27, 12, 0, 0, TimeSpan.Zero);

    private const string Note = "Raised the session lifetime on the gateway profile";

    private static OpenTicket Open() =>
        TicketRules.Create(Id, "VPN drops", "Disconnects every hour", Requester, Now);

    private static InProgressTicket InProgress() =>
        Assert.IsType<Assigned>(TicketRules.Assign(Open(), Agent, Now.AddMinutes(5))).Ticket;

    private static ResolvedTicket Resolved() =>
        TicketRules.Resolve(InProgress(), Note, Now.AddMinutes(10));

    private static ClosedTicket ClosedAt(DateTimeOffset closedAt) =>
        TicketRules.Close(Resolved(), closedAt);

    // ---- Assign ----

    [Fact]
    public void Assigning_an_open_ticket_puts_it_in_progress_with_that_agent()
    {
        var outcome = TicketRules.Assign(Open(), Agent, Now.AddMinutes(5));

        var inProgress = Assert.IsType<Assigned>(outcome).Ticket;
        Assert.Equal(Agent, inProgress.AssignedAgent);
        Assert.Equal(Now.AddMinutes(5), inProgress.AssignedAt);
        Assert.Equal(Id, inProgress.Id);
        Assert.Equal(Now, inProgress.CreatedAt);
    }

    [Fact]
    public void An_agent_cannot_be_assigned_their_own_ticket()
    {
        var requestersOwnAgentId = new AgentId(Requester.Value);

        var outcome = TicketRules.Assign(Open(), requestersOwnAgentId, Now.AddMinutes(5));

        Assert.IsType<AgentIsRequester>(outcome);
    }

    // ---- Resolve ----

    [Fact]
    public void Resolving_an_in_progress_ticket_records_the_note_and_keeps_the_agent()
    {
        var inProgress = InProgress();

        var resolved = TicketRules.Resolve(inProgress, Note, Now.AddMinutes(10));

        Assert.Equal(Note, resolved.ResolutionNote);
        Assert.Equal(Now.AddMinutes(10), resolved.ResolvedAt);
        Assert.Equal(inProgress.AssignedAgent, resolved.AssignedAgent);
        Assert.Equal(inProgress.AssignedAt, resolved.AssignedAt);
    }

    // ---- Close ----

    [Fact]
    public void Closing_a_resolved_ticket_keeps_its_resolution()
    {
        var resolved = Resolved();

        var closed = TicketRules.Close(resolved, Now.AddMinutes(15));

        Assert.Equal(Now.AddMinutes(15), closed.ClosedAt);
        Assert.Equal(resolved.ResolutionNote, closed.ResolutionNote);
        Assert.Equal(resolved.ResolvedAt, closed.ResolvedAt);
    }

    // ---- Reopen ----

    [Fact]
    public void Reopening_a_resolved_ticket_returns_it_to_the_same_agent_with_a_fresh_assignment()
    {
        var resolved = Resolved();
        var reopenedAt = Now.AddDays(1);

        var reopened = TicketRules.Reopen(resolved, reopenedAt);

        Assert.Equal(resolved.AssignedAgent, reopened.AssignedAgent);
        Assert.Equal(reopenedAt, reopened.AssignedAt);
    }

    [Fact]
    public void A_resolved_ticket_can_be_reopened_however_long_ago_it_was_resolved()
    {
        var reopened = TicketRules.Reopen(Resolved(), Now.AddDays(365));

        Assert.Equal(Now.AddDays(365), reopened.AssignedAt);
    }

    [Fact]
    public void A_closed_ticket_can_be_reopened_up_to_the_end_of_the_window()
    {
        var closedAt = Now.AddMinutes(15);

        var outcome = TicketRules.Reopen(ClosedAt(closedAt), closedAt + TicketRules.ReopenWindow);

        Assert.IsType<Reopened>(outcome);
    }

    [Fact]
    public void A_closed_ticket_cannot_be_reopened_after_the_window()
    {
        var closedAt = Now.AddMinutes(15);
        var afterWindow = closedAt + TicketRules.ReopenWindow + TimeSpan.FromSeconds(1);

        var outcome = TicketRules.Reopen(ClosedAt(closedAt), afterWindow);

        var elapsed = Assert.IsType<ReopenWindowElapsed>(outcome);
        Assert.Equal(closedAt + TicketRules.ReopenWindow, elapsed.ExpiredAt);
    }
}
