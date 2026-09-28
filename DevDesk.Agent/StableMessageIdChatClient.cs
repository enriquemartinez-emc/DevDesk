using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace DevDesk.Agent;

// OllamaSharp's streaming updates carry no MessageId, and the AG-UI converter mints a new message
// per update when it is missing - one message per token. Stamp one id per model turn instead.
public sealed class StableMessageIdChatClient(IChatClient inner) : DelegatingChatClient(inner)
{
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var turnMessageId = Guid.NewGuid().ToString("N");

        await foreach (var update in base.GetStreamingResponseAsync(messages, options, cancellationToken))
        {
            update.MessageId ??= turnMessageId;
            yield return update;
        }
    }
}
