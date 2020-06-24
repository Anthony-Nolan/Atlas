using Atlas.Common.Utils.Extensions;
using Atlas.HlaMetadataDictionary.ExternalInterface.Settings;
using Atlas.MatchingAlgorithm.DependencyInjection;
using Atlas.MatchingAlgorithm.Functions.DonorManagement;
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

            // TODO: ATLAS-327: Inject settings
            builder.Services.RegisterMatchingAlgorithmDonorManagement(DependencyInjectionUtils.OptionsReaderFor<HlaMetadataDictionarySettings>());
        }

        private static void RegisterSettings(IServiceCollection services)
        {
            services.RegisterOptions<HlaMetadataDictionarySettings>("HlaMetadataDictionary");
        }
    }
}