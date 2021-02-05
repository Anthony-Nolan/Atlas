using System;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Notifications;
using Atlas.Common.Utils.Extensions;
using Atlas.HlaMetadataDictionary.ExternalInterface.Settings;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
using Atlas.MatchingAlgorithm.Clients.AzureStorage;
using Atlas.MatchingAlgorithm.DependencyInjection;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using Atlas.MatchingAlgorithm.Services.Search;
using Atlas.MatchingAlgorithm.Settings;
using Atlas.MatchingAlgorithm.Settings.Azure;
using Atlas.MultipleAlleleCodeDictionary.Settings;
using Atlas.RepeatSearch.Clients;
using Atlas.RepeatSearch.Settings.ServiceBus;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.RepeatSearch.ExternalInterface.DependencyInjection
{
    public static class ServiceConfiguration
    {
        public static void RegisterRepeatSearch(
            this IServiceCollection services,
            Func<IServiceProvider, ApplicationInsightsSettings> fetchApplicationInsightsSettings,
            Func<IServiceProvider, AzureStorageSettings> fetchAzureStorageSettings,
            Func<IServiceProvider, HlaMetadataDictionarySettings> fetchHlaMetadataDictionarySettings,
            Func<IServiceProvider, MacDictionarySettings> fetchMacDictionarySettings,
            Func<IServiceProvider, MatchingConfigurationSettings> fetchMatchingConfigurationSettings,
            Func<IServiceProvider, MessagingServiceBusSettings> fetchMessagingServiceBusSettings,
            Func<IServiceProvider, NotificationsServiceBusSettings> fetchNotificationsServiceBusSettings,
            Func<IServiceProvider, string> fetchPersistentSqlConnectionString,
            Func<IServiceProvider, string> fetchRepeatSqlConnectionString,
            Func<IServiceProvider, string> fetchTransientASqlConnectionString,
            Func<IServiceProvider, string> fetchTransientBSqlConnectionString)
        {
            services.RegisterSettings(fetchApplicationInsightsSettings, fetchMessagingServiceBusSettings);
            services.RegisterServices();

            services.RegisterSearch(
                fetchApplicationInsightsSettings,
                fetchAzureStorageSettings,
                fetchHlaMetadataDictionarySettings,
                fetchMacDictionarySettings,
                //Matching algorithm doesn't require a service bus setting as these are handled by repeat search.
                _=> new MatchingAlgorithm.Settings.ServiceBus.MessagingServiceBusSettings(),
                fetchNotificationsServiceBusSettings,
                fetchMatchingConfigurationSettings,
                fetchPersistentSqlConnectionString,
                fetchTransientASqlConnectionString,
                fetchTransientBSqlConnectionString);
        }

        private static void RegisterSettings(
            this IServiceCollection services,
            Func<IServiceProvider, ApplicationInsightsSettings> fetchApplicationInsightsSettings,
            Func<IServiceProvider, MessagingServiceBusSettings> fetchMessagingServiceBusSettings)
        {
            services.MakeSettingsAvailableForUse(fetchApplicationInsightsSettings);
            services.MakeSettingsAvailableForUse(fetchMessagingServiceBusSettings);
        }

        private static void RegisterServices(
            this IServiceCollection services)
        {
            services.AddScoped<IRepeatSearchServiceBusClient, RepeatSearchServiceBusClient>();
            services.AddScoped<IResultsBlobStorageClient, ResultsBlobStorageClient>();
        }
    }
}