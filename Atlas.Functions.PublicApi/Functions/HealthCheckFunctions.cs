using Atlas.Common.Utils.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;

namespace Atlas.Functions.PublicApi.Functions
{
    public static class HealthCheckFunctions
    {
        [FunctionName(nameof(HealthCheck))]
        // ReSharper disable once UnusedParameter.Global
        public static OkObjectResult HealthCheck([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req)
        {
            return new OkObjectResult(HttpFunctionsConstants.HealthCheckResponse);
        }
    }
}