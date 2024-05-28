using Atlas.MatchPrediction.Test.Verification;
using AzureFunctions.Extensions.Swashbuckle;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Atlas.MatchPrediction.Test.Verification
{
    internal static class SwashBuckleStartup
    {
        public static void Configure(IServiceCollection services)
        {
            services.AddSwashBuckle(opts =>
            {
                opts.RoutePrefix = "api";
            },
                executingAssembly: Assembly.GetExecutingAssembly());
        }
    }
}