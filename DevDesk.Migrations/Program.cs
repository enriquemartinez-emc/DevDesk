using System.Reflection;
using DbUp;
using DevDesk.ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

using var host = builder.Build();
await host.StartAsync();

var connectionString =
    builder.Configuration.GetConnectionString(ServiceResources.Database)
    ?? throw new InvalidOperationException("Connection string 'devdeskdb' is not configured.");

var result = DeployChanges
    .To.PostgresqlDatabase(connectionString)
    .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
    .LogToConsole()
    .Build()
    .PerformUpgrade();

await host.StopAsync();

return result.Successful ? 0 : 1;
