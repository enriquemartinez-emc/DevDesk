using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace DevDesk.Server.Features.Chat;

// OllamaSharp streams updates without a MessageId, which AG-UI turns into one message per token.
public sealed class StableMessageIdChatClient(IChatClient inner) : DelegatingChatClient(inner)
{
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        var turnMessageId = Guid.NewGuid().ToString("N");

        await foreach (
            var update in base.GetStreamingResponseAsync(messages, options, cancellationToken)
        )
        {
            update.MessageId ??= turnMessageId;
            yield return update;
        }
    }
}
