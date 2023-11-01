using Atlas.ManualTesting.Common.Settings;
using Atlas.MatchPrediction.ExternalInterface.Settings;
using Atlas.MatchPrediction.Test.Validation;
using Atlas.MatchPrediction.Test.Validation.DependencyInjection;
using Atlas.MatchPrediction.Test.Validation.Models;
using Atlas.MatchPrediction.Test.Validation.Settings;
using Microsoft.ApplicationInsights.Extensibility.Implementation;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using static Atlas.Common.Utils.Extensions.DependencyInjectionUtils;

[assembly: FunctionsStartup(typeof(Startup))]

namespace Atlas.MatchPrediction.Test.Validation
{
    internal class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            // Stops the Visual Studio debug window from being flooded with not-very-helpful AI telemetry messages!
            TelemetryDebugWriter.IsTracingDisabled = true;

            RegisterSettings(builder.Services);

            builder.Services.RegisterValidationServices(
                OptionsReaderFor<OutgoingMatchPredictionRequestSettings>(),
                OptionsReaderFor<ValidationAzureStorageSettings>(),
                OptionsReaderFor<DataRefreshSettings>(),
                OptionsReaderFor<MessagingServiceBusSettings>(),
                OptionsReaderFor<MatchPredictionRequestsSettings>(),
                ConnectionStringReader("MatchPredictionValidation:Sql"),
                ConnectionStringReader("MatchPrediction:Sql"));
        }

        private static void RegisterSettings(IServiceCollection services)
        {
            services.RegisterAsOptions<OutgoingMatchPredictionRequestSettings>("OutgoingMatchPredictionRequests");
            services.RegisterAsOptions<ValidationAzureStorageSettings>("AzureStorage");
            services.RegisterAsOptions<DataRefreshSettings>("DataRefresh");
            services.RegisterAsOptions<MessagingServiceBusSettings>("MessagingServiceBus");
            services.RegisterAsOptions<MatchPredictionRequestsSettings>("MatchPredictionRequests");
        }
    }
}
