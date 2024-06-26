using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Reflection;
using Atlas.Common.Utils;
using Atlas.MatchingAlgorithm.Client.Models.Scoring;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using ServiceStatusModel = Atlas.Common.Utils.Models.ServiceStatus;

namespace Atlas.MatchingAlgorithm.Functions.Functions
{
    public class ServiceStatusFunctions
    {
        [SuppressMessage(null, SuppressMessage.UnusedParameter, Justification = SuppressMessage.UsedByAzureTrigger)]
        [Function(nameof(ServiceStatus))]
        [ProducesResponseType(typeof(ServiceStatusModel), (int)HttpStatusCode.OK)]
        public IActionResult ServiceStatus([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest request)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var name = assembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title;
            var version = assembly.GetName().Version?.ToString();

            return new JsonResult(new ServiceStatusModel
            {
                Name = name,
                Version = version
            });
        }
    }
}