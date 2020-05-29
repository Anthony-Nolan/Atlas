using System.Reflection;
using Atlas.Common.Utils;
using Atlas.Functions;
using AzureFunctions.Extensions.Swashbuckle;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;

[assembly: FunctionsStartup(typeof(SwashBuckleStartup))]
namespace Atlas.Functions
{
    internal class SwashBuckleStartup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            builder.AddSwashBuckle(Assembly.GetExecutingAssembly());
            UrlOpener.OpenInDefaultBrowser("http://localhost:7071/api/swagger/ui"); //'7071' is the default AzureFunctions port for all Azure functions.
        }
    }
}