using Atlas.ManualTesting.Common.Settings;
using Atlas.MatchPrediction.ExternalInterface.Settings;
using Atlas.MatchPrediction.Test.Validation;
using Atlas.MatchPrediction.Test.Validation.DependencyInjection;
using Atlas.MatchPrediction.Test.Validation.Models;
using Atlas.MatchPrediction.Test.Validation.Settings;
using Microsoft.ApplicationInsights.Extensibility.Implementation;
using Microsoft.Extensions.DependencyInjection;
using static Atlas.Common.Utils.Extensions.DependencyInjectionUtils;

namespace Atlas.MatchPrediction.Test.Validation
{
    internal static class Startup
    {
        public static void Configure(IServiceCollection services)
        {
            // Stops the Visual Studio debug window from being flooded with not-very-helpful AI telemetry messages!
            TelemetryDebugWriter.IsTracingDisabled = true;

            RegisterSettings(services);

            services.RegisterValidationServices(
                OptionsReaderFor<OutgoingMatchPredictionRequestSettings>(),
                OptionsReaderFor<ValidationAzureStorageSettings>(),
                OptionsReaderFor<DataRefreshSettings>(),
                OptionsReaderFor<MessagingServiceBusSettings>(),
                OptionsReaderFor<MatchPredictionRequestsSettings>(),
                OptionsReaderFor<ValidationSearchSettings>(),
                OptionsReaderFor<ValidationHomeworkSettings>(),
                ConnectionStringReader("MatchPredictionValidation:Sql"),
                ConnectionStringReader("MatchPrediction:Sql"),
                ConnectionStringReader("DonorImport:Sql"));
        }

        private static void RegisterSettings(IServiceCollection services)
        {
            services.RegisterAsOptions<OutgoingMatchPredictionRequestSettings>("OutgoingMatchPredictionRequests");
            services.RegisterAsOptions<ValidationAzureStorageSettings>("AzureStorage");
            services.RegisterAsOptions<DataRefreshSettings>("DataRefresh");
            services.RegisterAsOptions<ValidationHomeworkSettings>("Homework");
            services.RegisterAsOptions<MessagingServiceBusSettings>("MessagingServiceBus");
            services.RegisterAsOptions<MatchPredictionRequestsSettings>("MatchPredictionRequests");
            services.RegisterAsOptions<ValidationSearchSettings>("Search");
        }
    }
}
