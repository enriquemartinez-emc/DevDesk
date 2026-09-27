using Npgsql;

namespace DevDesk.Server.Features.Tickets;

public static class TicketsSchema
{
    public static async Task EnsureCreatedAsync(NpgsqlDataSource dataSource, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);

        const string createTableSql = """
            create table if not exists tickets (
                id uuid primary key,
                subject text not null,
                description text not null,
                requester_id uuid not null,
                created_at timestamptz not null,
                status text not null,
                assigned_agent uuid null,
                assigned_at timestamptz null,
                resolution_note text null,
                resolved_at timestamptz null,
                closed_at timestamptz null
            )
            """;

        await using var command = new NpgsqlCommand(createTableSql, connection);
        await command.ExecuteNonQueryAsync(ct);
    }
}
