using System;
using Atlas.Client.Models.Search.Results;
using Atlas.Common.Caching;
using Atlas.Common.ServiceBus.BatchReceiving;
using Atlas.Common.Utils.Extensions;
using Atlas.Functions.PublicApi.Test.Manual.Services;
using Atlas.Functions.PublicApi.Test.Manual.Services.ServiceBus;
using Atlas.Functions.PublicApi.Test.Manual.Settings;
using Atlas.MatchingAlgorithm.Common.Models;
using Microsoft.Extensions.DependencyInjection;
using static Atlas.Common.Utils.Extensions.DependencyInjectionUtils;

namespace Atlas.Functions.PublicApi.Test.Manual.DependencyInjection
{
    internal static class ServiceConfiguration
    {
        public static void RegisterServices(this IServiceCollection services)
        {
            services.RegisterSettings();
            services.RegisterInternalServices(
                OptionsReaderFor<MessagingServiceBusSettings>(),
                OptionsReaderFor<MatchingSettings>(),
                OptionsReaderFor<SearchSettings>()
                );
            services.RegisterLifeTimeScopedCacheTypes();
        }

        private static void RegisterSettings(this IServiceCollection services)
        {
            services.RegisterAsOptions<MessagingServiceBusSettings>("MessagingServiceBus");
            services.RegisterAsOptions<MatchingSettings>("Matching");
            services.RegisterAsOptions<SearchSettings>("Search");
        }

        private static void RegisterInternalServices(
            this IServiceCollection services,
            Func<IServiceProvider, MessagingServiceBusSettings> fetchMessagingServiceBusSettings,
            Func<IServiceProvider, MatchingSettings> fetchMatchingSettings,
            Func<IServiceProvider, SearchSettings> fetchSearchSettings
        )
        {
            services.AddSingleton<IMessageReceiverFactory, MessageReceiverFactory>(sp =>
                new MessageReceiverFactory(fetchMessagingServiceBusSettings(sp).ConnectionString)
            );
            services.AddSingleton<IDeadLetterReceiverFactory, DeadLetterReceiverFactory>(sp =>
                new DeadLetterReceiverFactory(fetchMessagingServiceBusSettings(sp).ConnectionString)
            );

            services.AddScoped(typeof(IMessagesPeeker<>), typeof(MessagesPeeker<>));
            services.AddScoped(typeof(IDeadLettersPeeker<>), typeof(DeadLettersPeeker<>));

            services.AddScoped<IDeadLettersPeeker<IdentifiedSearchRequest>, DeadLettersPeeker<IdentifiedSearchRequest>>(sp =>
            {
                var factory = sp.GetService<IDeadLetterReceiverFactory>();
                var settings = fetchMatchingSettings(sp);
                return new DeadLettersPeeker<IdentifiedSearchRequest>(factory, settings.RequestsTopic, settings.RequestsSubscription);
            });
            services.AddScoped<IMatchingRequestsPeeker, MatchingRequestsPeeker>();

            services.AddScoped<IMessagesPeeker<SearchResultsNotification>, MessagesPeeker<SearchResultsNotification>>(sp =>
            {
                var factory = sp.GetService<IMessageReceiverFactory>();
                var settings = fetchSearchSettings(sp);
                return new MessagesPeeker<SearchResultsNotification>(factory, settings.ResultsTopic, settings.ResultsSubscription);
            });
            services.AddScoped<ISearchResultNotificationsPeeker, SearchResultNotificationsPeeker>();
        }
    }
}