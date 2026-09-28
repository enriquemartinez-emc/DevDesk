using System.Text.RegularExpressions;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace DevDesk.Server.Features.Chat.Agent;

// A workflow hosted as an agent must accept the chat protocol, hence ChatProtocolExecutor.
public sealed partial class ClassifyQuestionExecutor(IChatClient chatClient)
    : ChatProtocolExecutor("classify")
{
    private const string Instructions = """
        Classify the support agent's latest message. Reply with exactly one word.
        Reply "research" if they ask how to fix or handle a problem, whether something has happened
        before, or what fixed it last time.
        Reply "conversation" for anything else: greetings, thanks, questions about one specific
        ticket by its id, or follow-ups about what was just said.
        """;

    [GeneratedRegex(@"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}")]
    private static partial Regex TicketId();

    protected override ProtocolBuilder ConfigureProtocol(ProtocolBuilder protocolBuilder) =>
        base.ConfigureProtocol(protocolBuilder).SendsMessage<RoutedQuestion>();

    protected override async ValueTask TakeTurnAsync(
        List<ChatMessage> messages,
        IWorkflowContext context,
        bool? emitEvents,
        CancellationToken cancellationToken = default
    )
    {
        var kind = await ClassifyAsync(messages.LatestUserText(), cancellationToken);
        await context.SendMessageAsync(new RoutedQuestion(messages, kind), cancellationToken);
    }

    private async Task<QuestionKind> ClassifyAsync(
        string question,
        CancellationToken cancellationToken
    )
    {
        if (await IsResearchAsync(question, cancellationToken))
        {
            return QuestionKind.Research;
        }

        return TicketId().IsMatch(question) ? QuestionKind.TicketLookup : QuestionKind.Conversation;
    }

    private async Task<bool> IsResearchAsync(string question, CancellationToken cancellationToken)
    {
        var reply = await chatClient.GetResponseAsync(
            [
                new ChatMessage(ChatRole.System, Instructions),
                new ChatMessage(ChatRole.User, question),
            ],
            new ChatOptions { Temperature = 0 },
            cancellationToken
        );

        return reply.Text.Contains("research", StringComparison.OrdinalIgnoreCase);
    }
}
