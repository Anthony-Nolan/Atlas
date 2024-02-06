using Atlas.Common.Utils.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;

namespace Atlas.MatchPrediction.Test.Verification.Functions
{
    public class HealthCheckFunctions
    {
        [FunctionName(nameof(HealthCheck))]
        public static OkObjectResult HealthCheck([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req)
        {
            return new OkObjectResult(HttpFunctionsConstants.HealthCheckResponse);
        }
    }
}
