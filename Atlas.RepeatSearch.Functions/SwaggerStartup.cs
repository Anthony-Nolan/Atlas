using System.Reflection;
using Atlas.RepeatSearch.Functions;
using AzureFunctions.Extensions.Swashbuckle;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.RepeatSearch.Functions
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