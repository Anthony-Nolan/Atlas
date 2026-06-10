using Atlas.SearchTracking.Common.Clients;
using Atlas.SearchTracking.Common.Settings.ServiceBus;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Atlas.SearchTracking.Common.DependencyInjection;

public static class SearchTrackingServiceBusRegistration
{
    /// <summary>
    /// Registers the search tracking Service Bus client.
    /// <para>
    /// The underlying <see cref="ServiceBusClient"/> and <see cref="ServiceBusSender"/> are registered as singletons:
    /// they are thread-safe and intended to be created once and reused for the lifetime of the application. Creating
    /// them per DI scope (as previously) opened - and leaked - a new AMQP connection per scope, exhausting the
    /// Service Bus connection quota.
    /// </para>
    /// <para>
    /// The wrapper itself stays scoped so it can continue to depend on the request-scoped <c>IAtlasLogger</c> without
    /// becoming a captive dependency.
    /// </para>
    /// Requires <see cref="SearchTrackingServiceBusSettings"/> to be registered (e.g. via <c>MakeSettingsAvailableForUse</c>).
    /// </summary>
    public static IServiceCollection RegisterSearchTrackingServiceBusClient(this IServiceCollection services)
    {
        services.TryAddKeyedSingleton<ServiceBusClient>(
            SearchTrackingServiceBusClient.ServiceBusKey,
            (sp, _) => new ServiceBusClient(sp.GetRequiredService<SearchTrackingServiceBusSettings>().ConnectionString));

        services.TryAddKeyedSingleton<ServiceBusSender>(
            SearchTrackingServiceBusClient.ServiceBusKey,
            (sp, key) => sp.GetRequiredKeyedService<ServiceBusClient>(key)
                .CreateSender(sp.GetRequiredService<SearchTrackingServiceBusSettings>().SearchTrackingTopic));

        services.AddScoped<ISearchTrackingServiceBusClient, SearchTrackingServiceBusClient>();

        return services;
    }
}