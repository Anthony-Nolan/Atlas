using Atlas.Common.ApplicationInsights;
using Atlas.Common.Utils.Extensions;
using Atlas.HlaMetadataDictionary.ExternalInterface.Settings;
using Atlas.MatchingAlgorithm.DependencyInjection;
using Atlas.MatchingAlgorithm.Functions.DonorManagement;
using Atlas.MultipleAlleleCodeDictionary.Settings;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(Startup))]

namespace Atlas.MatchingAlgorithm.Functions.DonorManagement
{
    internal class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            RegisterSettings(builder.Services);
            builder.Services.RegisterMatchingAlgorithmDonorManagement(
                DependencyInjectionUtils.OptionsReaderFor<ApplicationInsightsSettings>(),
                DependencyInjectionUtils.OptionsReaderFor<HlaMetadataDictionarySettings>(),
                DependencyInjectionUtils.OptionsReaderFor<MacDictionarySettings>()
            );
        }

        private static void RegisterSettings(IServiceCollection services)
        {
            services.RegisterOptions<ApplicationInsightsSettings>("ApplicationInsights");
            services.RegisterOptions<HlaMetadataDictionarySettings>("HlaMetadataDictionary");
            services.RegisterOptions<MacDictionarySettings>("MacDictionary");
        }
    }
}