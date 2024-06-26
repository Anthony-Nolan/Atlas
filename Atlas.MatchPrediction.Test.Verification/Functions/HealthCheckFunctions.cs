using Atlas.Common.Utils.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Atlas.MatchPrediction.Test.Verification.Functions
{
    public class HealthCheckFunctions
    {
        [Function(nameof(HealthCheck))]
        public static OkObjectResult HealthCheck([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req)
        {
            return new OkObjectResult(HttpFunctionsConstants.HealthCheckResponse);
        }
    }
}
