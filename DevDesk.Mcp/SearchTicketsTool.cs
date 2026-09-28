using System.ComponentModel;
using Dapper;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using Npgsql;

namespace DevDesk.Mcp;

[McpServerToolType]
public static class SearchTicketsTool
{
    [
        McpServerTool(Name = ToolNames.SearchTickets),
        Description(
            "Search existing tickets, of any status, for similar problems, including how resolved ones were fixed. "
                + "Use it for \"has this come up before?\" and \"what fixed it last time?\" questions."
        )
    ]
    public static async Task<string> SearchTickets(
        [Description("Keywords describing the problem, e.g. \"vpn disconnects every hour\"")]
            string query,
        NpgsqlDataSource dataSource,
        IOptions<SearchOptions> options,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return "Give some keywords describing the problem to search for.";
        }

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        // plainto_tsquery sanitizes free text; the replace turns its AND into OR, because a question is
        // rarely an exact phrase. ts_rank puts the tickets matching the most keywords first.
        const string searchSql = $"""
            with documents as (
                select t.*,
                       to_tsvector('english',
                           t.subject || ' ' || t.description || ' ' || coalesce(t.resolution_note, '')) as document
                from tickets t
            ),
            search as (
                select replace(plainto_tsquery('english', @Query)::text, ' & ', ' | ')::tsquery as query
            )
            select {TicketRow.Columns}
            from documents d, search s
            where d.document @@ s.query
            order by ts_rank(d.document, s.query) desc, d.created_at desc
            limit @MaxResults
            """;

        var tickets = (
            await connection.QueryAsync<TicketRow>(
                new CommandDefinition(
                    searchSql,
                    new { Query = query, MaxResults = options.Value.TicketResults },
                    cancellationToken: cancellationToken
                )
            )
        ).ToList();

        if (tickets.Count == 0)
        {
            return "No similar tickets found.";
        }

        return string.Join(
            "\n",
            tickets.Select(ticket =>
                $"- Ticket {ticket.Id} ('{ticket.Subject}', {ticket.Status}): {ticket.Description}"
                + (
                    ticket.ResolutionNote is null
                        ? " No resolution recorded yet."
                        : $" Resolution: {ticket.ResolutionNote}"
                )
            )
        );
    }
}
