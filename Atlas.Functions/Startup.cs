using Atlas.Common.ApplicationInsights;
using Atlas.Common.Utils.Extensions;
using Atlas.Functions;
using Atlas.MatchingAlgorithm.DependencyInjection;
using Atlas.MatchingAlgorithm.Settings.ServiceBus;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface.DependencyInjection;
using Atlas.MultipleAlleleCodeDictionary.Settings;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using static Atlas.Common.Utils.Extensions.DependencyInjectionUtils;

[assembly: FunctionsStartup(typeof(Startup))]
namespace Atlas.Functions
{
    internal class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            RegisterSettings(builder.Services);
            builder.Services.RegisterMatchingAlgorithmOrchestration(OptionsReaderFor<MessagingServiceBusSettings>());
            builder.Services.RegisterMacDictionary(
                OptionsReaderFor<ApplicationInsightsSettings>(),
                OptionsReaderFor<MacDictionarySettings>()
            );
        }

        private static void RegisterSettings(IServiceCollection services)
        {
            services.RegisterAsOptions<ApplicationInsightsSettings>("ApplicationInsights");
            services.RegisterAsOptions<MacDictionarySettings>("MacDictionary");
            services.RegisterAsOptions<MessagingServiceBusSettings>("MessagingServiceBus");
        }
    }
}