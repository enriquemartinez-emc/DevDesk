using DevDesk.Server.Common;

namespace DevDesk.Server.Features.Tickets;

public static class TicketsEndpoints
{
    private const string Tag = "Tickets";

    public static IEndpointRouteBuilder MapTicketsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ApiRoutes.Tickets).WithTags(Tag);

        group.MapCreateTicketEndpoint();
        group.MapGetTicketEndpoint();
        group.MapListTicketsEndpoint();
        group.MapAssignTicketEndpoint();
        group.MapResolveTicketEndpoint();
        group.MapCloseTicketEndpoint();
        group.MapReopenTicketEndpoint();

        return app;
    }
}
