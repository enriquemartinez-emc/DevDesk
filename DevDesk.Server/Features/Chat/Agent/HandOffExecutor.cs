using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace DevDesk.Server.Features.Chat.Agent;

[SendsMessage(typeof(List<ChatMessage>))]
[SendsMessage(typeof(TurnToken))]
public sealed class HandOffExecutor(string id) : Executor<RoutedQuestion>(id)
{
    public override async ValueTask HandleAsync(
        RoutedQuestion message,
        IWorkflowContext context,
        CancellationToken cancellationToken = default
    )
    {
        await context.SendMessageAsync(message.Messages, cancellationToken);
        await context.SendMessageAsync(new TurnToken(emitEvents: true), cancellationToken);
    }
}
