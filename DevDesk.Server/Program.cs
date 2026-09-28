using DevDesk.Server.Features.Chat;
using DevDesk.Server.Features.Tickets;
using FluentValidation;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.AddNpgsqlDataSource("devdeskdb");
builder.Services.AddChatProxy();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

await TicketsSchema.EnsureCreatedAsync(
    app.Services.GetRequiredService<NpgsqlDataSource>(),
    CancellationToken.None
);

app.MapTicketsEndpoints();
app.MapChatProxy();

app.MapDefaultEndpoints();

app.UseFileServer();

app.Run();
