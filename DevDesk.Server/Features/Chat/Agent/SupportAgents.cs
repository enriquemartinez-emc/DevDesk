using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;

namespace DevDesk.Server.Features.Chat.Agent;

public static class SupportAgents
{
    private const string LookupInstructions = """
        You are a support-desk assistant for DevDesk. Use lookup_ticket to answer the question about
        the ticket whose id the support agent gave.
        """;

    private const string ConversationInstructions = """
        You are a support-desk assistant for DevDesk. Reply briefly and politely to greetings and
        follow-ups. You can't look tickets up here: if asked about a specific ticket, ask for its id;
        if asked how to fix a problem, ask for a one-sentence description of it.
        """;

    private const string SynthesisInstructions = """
        You are a support-desk assistant for DevDesk. Answer the support agent's question using only
        the findings in the message: past tickets and runbook passages. Cite ticket ids and runbook
        file names. If the findings don't answer the question, say so plainly; don't invent steps.
        """;

    public static AIAgent Lookup(IChatClient chatClient, IEnumerable<McpClientTool> mcpTools) =>
        chatClient.AsAIAgent(
            name: "lookup",
            instructions: LookupInstructions,
            tools:
            [
                .. mcpTools.Where(tool => tool.Name == SupportTools.LookupTicket).Cast<AITool>(),
            ]
        );

    public static AIAgent Conversation(IChatClient chatClient) =>
        chatClient.AsAIAgent(name: "conversation", instructions: ConversationInstructions);

    public static AIAgent Synthesis(IChatClient chatClient) =>
        chatClient.AsAIAgent(name: "synthesis", instructions: SynthesisInstructions);
}
