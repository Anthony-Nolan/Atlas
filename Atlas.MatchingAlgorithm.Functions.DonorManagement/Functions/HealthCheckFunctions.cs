using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Atlas.MatchingAlgorithm.Functions.DonorManagement.Functions
{
    public class HealthCheckFunctions
    {
        private readonly HealthCheckService healthCheckService;

        public HealthCheckFunctions(HealthCheckService healthCheckService)
        {
            this.healthCheckService = healthCheckService;
        }

        [FunctionName(nameof(HealthCheck))]
        public async Task<IActionResult> HealthCheck([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest request)
        {
            var healthStatus = await healthCheckService.CheckHealthAsync();
            return new OkObjectResult(Enum.GetName(typeof(HealthStatus), healthStatus.Status));
        }
    }
}
