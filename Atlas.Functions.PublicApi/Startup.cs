using Atlas.Common.ApplicationInsights;
using Atlas.Common.Notifications;
using Atlas.Functions.PublicApi;
using Atlas.MatchingAlgorithm.DependencyInjection;
using Atlas.MatchingAlgorithm.Settings.ServiceBus;
using Atlas.RepeatSearch.Settings.ServiceBus;
using Atlas.MatchPrediction.ExternalInterface.DependencyInjection;
using Atlas.RepeatSearch.DependencyInjection;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using static Atlas.Common.Utils.Extensions.DependencyInjectionUtils;

[assembly: FunctionsStartup(typeof(Startup))]

namespace Atlas.Functions.PublicApi
{
    internal class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            RegisterSettings(builder.Services);

            builder.Services.RegisterMatchingAlgorithmOrchestration(OptionsReaderFor<MatchingAlgorithm.Settings.ServiceBus.MessagingServiceBusSettings>());

            builder.Services.RegisterRepeatSearchOrchestration(OptionsReaderFor<RepeatSearch.Settings.ServiceBus.MessagingServiceBusSettings>());

            builder.Services.RegisterMatchPredictionValidator();
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
        }
    }
}