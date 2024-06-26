using System.Diagnostics.CodeAnalysis;
using Atlas.Common.Utils;
using Atlas.Common.Utils.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Atlas.RepeatSearch.Functions.Functions
{
    public class HealthCheckFunctions
    {
        [SuppressMessage(null, SuppressMessage.UnusedParameter, Justification = SuppressMessage.UsedByAzureTrigger)]
        [Function(nameof(HealthCheck))]
        public static OkObjectResult HealthCheck([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req)
        {
            return new OkObjectResult(HttpFunctionsConstants.HealthCheckResponse);
        }
    }
}