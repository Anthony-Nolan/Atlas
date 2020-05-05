using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.AspNetCore.Http;

namespace Atlas.MatchPrediction.Functions.Functions
{
    public class HealthCheck
    {
        [FunctionName("HealthCheck")]
        public static OkObjectResult Run([HttpTrigger] HttpRequest req)
        {
            string responseMessage = "This HTTP triggered function executed successfully.";
            return new OkObjectResult(responseMessage);
        }
    }
}