using Atlas.MatchPrediction.DependencyInjection;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(Atlas.MatchPrediction.Functions.Startup))]

namespace Atlas.MatchPrediction.Functions
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.RegisterMatchPredictionServices();
            builder.Services.RegisterFunctionsAppSettings();
        }
    }
}