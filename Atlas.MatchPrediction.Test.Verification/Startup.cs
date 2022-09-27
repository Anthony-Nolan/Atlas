using Atlas.Common.ApplicationInsights;
using Atlas.HlaMetadataDictionary.ExternalInterface.Settings;
using Atlas.MatchingAlgorithm.DependencyInjection;
using Atlas.MatchPrediction.Test.Verification;
using Atlas.MatchPrediction.Test.Verification.DependencyInjection;
using Atlas.MatchPrediction.Test.Verification.Settings;
using Atlas.MultipleAlleleCodeDictionary.Settings;
using Microsoft.ApplicationInsights.Extensibility.Implementation;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using static Atlas.Common.Utils.Extensions.DependencyInjectionUtils;

[assembly: FunctionsStartup(typeof(Startup))]

namespace Atlas.MatchPrediction.Test.Verification
{
    internal class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            // Stops the Visual Studio debug window from being flooded with not-very-helpful AI telemetry messages!
            TelemetryDebugWriter.IsTracingDisabled = true;

            RegisterSettings(builder.Services);

            builder.Services.RegisterVerificationServices(
                ConnectionStringReader("MatchPredictionVerification:Sql"),
                ConnectionStringReader("MatchPrediction:Sql"),
                ConnectionStringReader("DonorImport:Sql"),
                OptionsReaderFor<VerificationAzureStorageSettings>(),
                OptionsReaderFor<HlaMetadataDictionarySettings>(),
                OptionsReaderFor<ApplicationInsightsSettings>(),
                OptionsReaderFor<MacDictionarySettings>(),
                OptionsReaderFor<MacDownloadSettings>());

            builder.Services.RegisterMatchingAlgorithmScoring(
                OptionsReaderFor<ApplicationInsightsSettings>(),
                OptionsReaderFor<HlaMetadataDictionarySettings>(),
                OptionsReaderFor<MacDictionarySettings>(),
                ConnectionStringReader("Matching:PersistentSql")
            );
        }

        private static void RegisterSettings(IServiceCollection services)
        {
            services.RegisterAsOptions<ApplicationInsightsSettings>("ApplicationInsights");
            services.RegisterAsOptions<VerificationAzureStorageSettings>("AzureStorage");
            services.RegisterAsOptions<HlaMetadataDictionarySettings>("HlaMetadataDictionary");
            services.RegisterAsOptions<MacDictionarySettings>("MacDictionary");
            services.RegisterAsOptions<MacDownloadSettings>("MacDictionary:Download");
        }
    }
}
