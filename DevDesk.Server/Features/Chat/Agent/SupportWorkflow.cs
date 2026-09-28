using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace DevDesk.Server.Features.Chat.Agent;

public static class SupportWorkflow
{
    public static Workflow Build(IChatClient chatClient, McpConnection mcp)
    {
        var classify = new ClassifyQuestionExecutor(chatClient).BindExecutor();
        var research = new ResearchExecutor(mcp.Client).BindExecutor();
        var handOffToLookup = new HandOffExecutor("hand-off-to-lookup").BindExecutor();
        var handOffToConversation = new HandOffExecutor("hand-off-to-conversation").BindExecutor();

        var synthesis = SupportAgents.Synthesis(chatClient).BindAsExecutor(emitEvents: true);
        var lookup = SupportAgents.Lookup(chatClient, mcp.Tools).BindAsExecutor(emitEvents: true);
        var conversation = SupportAgents.Conversation(chatClient).BindAsExecutor(emitEvents: true);

        return new WorkflowBuilder(classify)
            .Route(classify, QuestionKind.Research, research)
            .Route(classify, QuestionKind.TicketLookup, handOffToLookup)
            .Route(classify, QuestionKind.Conversation, handOffToConversation)
            .AddEdge(research, synthesis)
            .AddEdge(handOffToLookup, lookup)
            .AddEdge(handOffToConversation, conversation)
            .WithOutputFrom(synthesis, lookup, conversation)
            .Build();
    }

    private static WorkflowBuilder Route(
        this WorkflowBuilder builder,
        ExecutorBinding from,
        QuestionKind kind,
        ExecutorBinding to
    ) => builder.AddEdge<RoutedQuestion>(from, to, question => question?.Kind == kind);
}
