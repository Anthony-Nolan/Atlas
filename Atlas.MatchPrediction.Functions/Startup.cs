using Atlas.Common.Utils.Extensions;
using Atlas.HlaMetadataDictionary.ExternalInterface.Settings;
using Atlas.MatchPrediction.DependencyInjection;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(Atlas.MatchPrediction.Functions.Startup))]

namespace Atlas.MatchPrediction.Functions
{
    internal class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            RegisterSettings(builder.Services);
            builder.Services.RegisterMatchPredictionServices(
                DependencyInjectionUtils.OptionsReaderFor<HlaMetadataDictionarySettings>()
            );
        }

        private static void RegisterSettings(IServiceCollection services)
        {
            services.RegisterOptions<HlaMetadataDictionarySettings>("HlaMetadataDictionary");
        }
    }
}