using AzureFunctions.Extensions.Swashbuckle;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using System.Reflection;
using Atlas.MatchPrediction.Test.Validation;

[assembly: WebJobsStartup(typeof(SwashBuckleStartup))]

namespace Atlas.MatchPrediction.Test.Validation
{
    internal class SwashBuckleStartup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            builder.AddSwashBuckle(Assembly.GetExecutingAssembly());
        }
    }
}