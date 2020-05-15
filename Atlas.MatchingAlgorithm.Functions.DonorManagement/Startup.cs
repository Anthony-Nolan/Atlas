using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.MatchingAlgorithm.DependencyInjection;
using Atlas.MatchingAlgorithm.Functions.DonorManagement;
using Atlas.MatchingAlgorithm.Settings;
using Atlas.MatchingAlgorithm.Settings.Azure;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

[assembly: FunctionsStartup(typeof(Startup))]
namespace Atlas.MatchingAlgorithm.Functions.DonorManagement
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.RegisterSettingsForDonorManagementFunctionsApp();
            builder.Services.RegisterDataServices();
            builder.Services.RegisterHlaMetadataDictionary(
                sp => sp.GetService<IOptions<AzureStorageSettings>>().Value.ConnectionString,
                sp => sp.GetService<IOptions<WmdaSettings>>().Value.WmdaFileUri,
                sp => sp.GetService<IOptions<HlaServiceSettings>>().Value.ApiKey,
                sp => sp.GetService<IOptions<HlaServiceSettings>>().Value.BaseUrl,
                sp => sp.GetService<IOptions<ApplicationInsightsSettings>>().Value.InstrumentationKey
                );
            builder.Services.RegisterSearchAlgorithmTypes();
            builder.Services.RegisterDonorManagementServices();
        }
    }
}