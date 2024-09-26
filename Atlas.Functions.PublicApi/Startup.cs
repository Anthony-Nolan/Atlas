using Atlas.Common.ApplicationInsights;
using Atlas.Common.Notifications;
using Atlas.MatchingAlgorithm.DependencyInjection;
using Atlas.MatchPrediction.ExternalInterface.DependencyInjection;
using Atlas.RepeatSearch.ExternalInterface.DependencyInjection;
using Atlas.SearchTracking.Common.Settings.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using static Atlas.Common.Utils.Extensions.DependencyInjectionUtils;

namespace Atlas.Functions.PublicApi
{
    internal static class Startup
    {
        public static void Configure(IServiceCollection services)
        {
            RegisterSettings(services);

            services.RegisterMatchingAlgorithmOrchestration(
                OptionsReaderFor<MatchingAlgorithm.Settings.ServiceBus.MessagingServiceBusSettings>(),
                OptionsReaderFor<SearchTrackingServiceBusSettings>());

            services.RegisterRepeatSearchOrchestration(OptionsReaderFor<RepeatSearch.Settings.ServiceBus.MessagingServiceBusSettings>(),
                OptionsReaderFor<SearchTrackingServiceBusSettings>());

            services.RegisterMatchPredictionValidator();
        }

        private static void RegisterSettings(IServiceCollection services)
        {
            // Shared settings
            services.RegisterAsOptions<ApplicationInsightsSettings>("ApplicationInsights");
            services.RegisterAsOptions<NotificationsServiceBusSettings>("NotificationsServiceBus");

            // Matching Algorithm - initiation services only
            services.RegisterAsOptions<MatchingAlgorithm.Settings.ServiceBus.MessagingServiceBusSettings>("Matching:MessagingServiceBus");

            // Repeat Search - initiation services only
            services.RegisterAsOptions<RepeatSearch.Settings.ServiceBus.MessagingServiceBusSettings>("RepeatSearch:MessagingServiceBus");

            services.RegisterAsOptions<SearchTrackingServiceBusSettings>("SearchTracking:SearchTrackingServiceBus");
        }
    }
}