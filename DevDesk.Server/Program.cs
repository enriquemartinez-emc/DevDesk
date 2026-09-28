using DevDesk.Server.Features.Chat;
using DevDesk.Server.Features.Tickets;
using FluentValidation;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddProblemDetails();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.AddNpgsqlDataSource("devdeskdb");
await using var mcp = await McpConnection.ConnectAsync(builder.Configuration);
builder.AddSupportChat(mcp);

// Keeps /agui and the DevUI endpoints out of the document the frontend client is generated from.
builder.Services.AddOpenApi(options =>
    options.ShouldInclude = api => api.RelativePath?.StartsWith("api/") == true
);

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options => options.WithTitle("DevDesk API"));
}

app.MapTicketsEndpoints();
app.MapSupportChat();

app.MapDefaultEndpoints();

app.UseFileServer();

app.Run();
