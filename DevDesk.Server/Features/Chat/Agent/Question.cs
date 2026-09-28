using Microsoft.Extensions.AI;

namespace DevDesk.Server.Features.Chat.Agent;

public enum QuestionKind
{
    Research,
    TicketLookup,
    Conversation,
}

public sealed record RoutedQuestion(List<ChatMessage> Messages, QuestionKind Kind);

public static class ChatMessages
{
    public static string LatestUserText(this IEnumerable<ChatMessage> messages) =>
        messages.LastOrDefault(message => message.Role == ChatRole.User)?.Text ?? string.Empty;
}
