using Atlas.Common.ApplicationInsights;
using Atlas.Common.AzureStorage;
using Atlas.Common.Utils.Extensions;
using Atlas.Functions;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Settings;
using Atlas.MatchingAlgorithm.DependencyInjection;
using Atlas.MultipleAlleleCodeDictionary.DependencyInjection;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

[assembly: FunctionsStartup(typeof(Startup))]

namespace Atlas.Functions
{
    internal class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            RegisterSettings(builder);

            builder.Services.RegisterMatchingAlgorithmOrchestration();
            builder.Services.RegisterMacDictionaryImportTypes();
            builder.Services.RegisterHlaMetadataDictionaryForRecreation(sp => sp.GetService<IOptions<AzureStorageSettings>>().Value.ConnectionString,
                sp => sp.GetService<IOptions<WmdaSettings>>().Value.WmdaFileUri,
                sp => sp.GetService<IOptions<ApplicationInsightsSettings>>().Value);
        }

        private static void RegisterSettings(IFunctionsHostBuilder builder)
        {
            builder.Services.RegisterOptions<ApplicationInsightsSettings>("ApplicationInsights");
            builder.Services.RegisterOptions<AzureStorageSettings>("AzureStorage");
            builder.Services.RegisterOptions<WmdaSettings>("Wmda");
        }
    }
}