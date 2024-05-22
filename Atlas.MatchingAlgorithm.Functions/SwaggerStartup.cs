using System.Reflection;
using Atlas.MatchingAlgorithm.Functions;
using AzureFunctions.Extensions.Swashbuckle;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.MatchingAlgorithm.Functions
{
    internal static class SwashBuckleStartup 
    {
        public static void Configure(IServiceCollection services)
        {
            services.AddSwashBuckle(Assembly.GetExecutingAssembly());
        }
    }
}