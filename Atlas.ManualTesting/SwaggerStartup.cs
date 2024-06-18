using System.Reflection;
using Atlas.ManualTesting;
using AzureFunctions.Extensions.Swashbuckle;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.ManualTesting
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