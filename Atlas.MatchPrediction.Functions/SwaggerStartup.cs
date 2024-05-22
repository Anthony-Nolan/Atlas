using System.Reflection;
using Atlas.MatchPrediction.Functions;
using AzureFunctions.Extensions.Swashbuckle;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.MatchPrediction.Functions
{
    internal static class SwashBuckleStartup
    {
        public static void Configure(IServiceCollection services)
        {
            services.AddSwashBuckle(Assembly.GetExecutingAssembly());
        }
    }
}