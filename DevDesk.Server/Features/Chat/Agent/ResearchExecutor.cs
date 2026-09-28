using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace DevDesk.Server.Features.Chat.Agent;

[SendsMessage(typeof(ChatMessage))]
[SendsMessage(typeof(TurnToken))]
public sealed class ResearchExecutor(McpClient mcpClient) : Executor<RoutedQuestion>("research")
{
    public override async ValueTask HandleAsync(
        RoutedQuestion message,
        IWorkflowContext context,
        CancellationToken cancellationToken = default
    )
    {
        var question = message.Messages.LatestUserText();

        var tickets = SearchAsync(context, SupportTools.SearchTickets, question, cancellationToken);
        var runbooks = SearchAsync(
            context,
            SupportTools.SearchRunbooks,
            question,
            cancellationToken
        );

        var findings = new ChatMessage(
            ChatRole.User,
            $"Question: {question}\n\nPast tickets:\n{await tickets}\n\nRunbook passages:\n{await runbooks}"
        );

        await context.SendMessageAsync(findings, cancellationToken);
        await context.SendMessageAsync(new TurnToken(emitEvents: true), cancellationToken);
    }

    // Reported as a tool call so the chat UI shows the search while it runs.
    private async Task<string> SearchAsync(
        IWorkflowContext context,
        string tool,
        string question,
        CancellationToken cancellationToken
    )
    {
        var arguments = new Dictionary<string, object?> { ["query"] = question };
        var call = new FunctionCallContent(NewId(), tool, arguments);
        await ReportAsync(context, ChatRole.Assistant, call, cancellationToken);

        var result = await mcpClient.CallToolAsync(
            tool,
            arguments,
            cancellationToken: cancellationToken
        );
        var text = string.Join("\n", result.Content.OfType<TextContentBlock>().Select(b => b.Text));

        var callResult = new FunctionResultContent(call.CallId, text);
        await ReportAsync(context, ChatRole.Tool, callResult, cancellationToken);
        return text;
    }

    private ValueTask ReportAsync(
        IWorkflowContext context,
        ChatRole role,
        AIContent content,
        CancellationToken cancellationToken
    ) =>
        context.AddEventAsync(
            new AgentResponseUpdateEvent(
                Id,
                new AgentResponseUpdate(role, [content]) { MessageId = NewId() }
            ),
            cancellationToken
        );

    private static string NewId() => Guid.NewGuid().ToString("N");
}
