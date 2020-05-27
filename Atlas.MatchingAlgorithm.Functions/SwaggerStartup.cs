using System.Reflection;
using Atlas.MatchingAlgorithm.Functions;
using AzureFunctions.Extensions.Swashbuckle;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;

[assembly: FunctionsStartup(typeof(SwashBuckleStartup))]
namespace Atlas.MatchingAlgorithm.Functions
{
    internal class SwashBuckleStartup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            builder.AddSwashBuckle(Assembly.GetExecutingAssembly());
        }
    }
}