using Atlas.SearchTracking.Data.Context;
using Atlas.Utilities.RerunFailedSearches;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;

// Usage:
//   Atlas.Utilities.RerunFailedSearches --from <UTC date> [--only-parallel-failures] [--forced-parallel true|false]
// Connection strings + topic names are read from appsettings.json (see appsettings.template.json) or
// environment variables under the "Rerun" section.

var inputs = RerunInputs.Parse(args);

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var settings = configuration.GetSection("Rerun").Get<RerunSettings>() ?? new RerunSettings();

if (string.IsNullOrWhiteSpace(settings.ServiceBusConnectionString) ||
    settings.ServiceBusConnectionString.Equals("override-this", StringComparison.OrdinalIgnoreCase) ||
    string.IsNullOrWhiteSpace(settings.SearchTrackingConnectionString))
{
    throw new InvalidOperationException(
        "Rerun:ServiceBusConnectionString and Rerun:SearchTrackingConnectionString must be set " +
        "(copy appsettings.template.json to appsettings.json and fill them in, or provide them as environment variables).");
}

await using var serviceBusClient = new ServiceBusClient(settings.ServiceBusConnectionString);
var contextFactory = new ContextFactory();

var runner = new RerunFailedSearchesRunner(
    new FailedSearchNotificationReader(serviceBusClient),
    new FailedSearchTrackingReader(() => contextFactory.Create(settings.SearchTrackingConnectionString)),
    new FailedSearchResubmitter(serviceBusClient, settings),
    settings,
    Console.Out);

await runner.Run(inputs);
