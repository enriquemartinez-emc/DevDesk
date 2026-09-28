using System.Net;
using System.Net.Http.Json;

namespace DevDesk.Mcp.Clients;

public sealed class TicketsApiClient(HttpClient httpClient)
{
    public async Task<TicketDto?> GetTicketAsync(Guid ticketId, CancellationToken ct)
    {
        var response = await httpClient.GetAsync($"/api/tickets/{ticketId}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TicketDto>(ct);
    }
}

public sealed record TicketDto(
    Guid Id,
    string Subject,
    string Description,
    Guid RequesterId,
    DateTimeOffset CreatedAt,
    string Status,
    Guid? AssignedAgent,
    DateTimeOffset? AssignedAt,
    string? ResolutionNote,
    DateTimeOffset? ResolvedAt,
    DateTimeOffset? ClosedAt
);
