using Atlas.MatchingAlgorithm.DependencyInjection;
using Atlas.MatchingAlgorithm.Functions.DonorManagement;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(Startup))]

namespace Atlas.MatchingAlgorithm.Functions.DonorManagement
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.RegisterSettingsForDonorManagementFunctionsApp();
            builder.Services.RegisterDataServices();
            builder.Services.RegisterTypesNeededForHlaMetadataDictionary();
            builder.Services.RegisterSearchAlgorithmTypes();
            builder.Services.RegisterDonorManagementServices();
        }
    }
}