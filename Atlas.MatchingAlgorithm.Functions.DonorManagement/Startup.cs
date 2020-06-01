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
            // TODO: ATLAS-327: Inject settings
            builder.Services.RegisterMatchingAlgorithmDonorManagement();
        }
    }
}