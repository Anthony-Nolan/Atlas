using AzureFunctions.Extensions.Swashbuckle;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.MatchPrediction.Test.Validation
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