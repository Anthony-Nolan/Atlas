using Atlas.Common.ApplicationInsights;
using Atlas.Common.Utils.Extensions;
using Atlas.Functions;
using Atlas.MatchingAlgorithm.DependencyInjection;
using Atlas.MatchingAlgorithm.Settings.ServiceBus;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface.DependencyInjection;
using Atlas.MultipleAlleleCodeDictionary.Settings;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(Startup))]

namespace Atlas.Functions
{
    internal class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            RegisterSettings(builder.Services);
            builder.Services.RegisterMatchingAlgorithmOrchestration(DependencyInjectionUtils.OptionsReaderFor<MessagingServiceBusSettings>());
            builder.Services.RegisterMacDictionary(
                DependencyInjectionUtils.OptionsReaderFor<ApplicationInsightsSettings>(),
                DependencyInjectionUtils.OptionsReaderFor<MacDictionarySettings>()
            );
        }

        private static void RegisterSettings(IServiceCollection services)
        {
            services.RegisterOptions<ApplicationInsightsSettings>("ApplicationInsights");
            services.RegisterOptions<MacDictionarySettings>("MacDictionary");
            services.RegisterOptions<MessagingServiceBusSettings>("MessagingServiceBus");
        }
    }
}