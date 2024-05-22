using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Atlas.Common.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using ServiceStatusModel = Atlas.Common.Utils.Models.ServiceStatus;

namespace Atlas.MatchingAlgorithm.Functions.Functions
{
    public class ServiceStatusFunctions
    {
        [SuppressMessage(null, SuppressMessage.UnusedParameter, Justification = SuppressMessage.UsedByAzureTrigger)]
        [Function(nameof(ServiceStatus))]
        public ServiceStatusModel ServiceStatus([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest request)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var name = assembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title;
            var version = assembly.GetName().Version?.ToString();

            return new ServiceStatusModel
            {
                Name = name,
                Version = version
            };
        }
    }
}