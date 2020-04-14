using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using ServiceStatusModel = Nova.Utils.Models.ServiceStatus;

namespace Atlas.MatchingAlgorithm.Functions.Functions
{
    public class ServiceStatus
    {
        [FunctionName("ServiceStatus")]
        public ServiceStatusModel RunSearch([HttpTrigger] HttpRequest request)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var name = assembly.GetCustomAttribute<AssemblyTitleAttribute>().Title;
            var version = assembly.GetName().Version.ToString();

            return new ServiceStatusModel
            {
                Name = name,
                Version = version
            };
        }
    }
}