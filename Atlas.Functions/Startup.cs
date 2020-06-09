using Atlas.Functions;
using Atlas.MatchingAlgorithm.DependencyInjection;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(Startup))]

namespace Atlas.Functions
{
    internal class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.RegisterMatchingAlgorithmOrchestration();
            builder.Services.RegisterMacDictionary();
        }
    }
}