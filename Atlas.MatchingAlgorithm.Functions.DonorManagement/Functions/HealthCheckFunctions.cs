using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Azure.Functions.Worker;

namespace Atlas.MatchingAlgorithm.Functions.DonorManagement.Functions
{
    public class HealthCheckFunctions
    {
        private readonly HealthCheckService healthCheckService;

        public HealthCheckFunctions(HealthCheckService healthCheckService)
        {
            this.healthCheckService = healthCheckService;
        }

        [Function(nameof(HealthCheck))]
        public async Task<IActionResult> HealthCheck([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest request)
        {
            var healthStatus = await healthCheckService.CheckHealthAsync();
            return new JsonResult(Enum.GetName(typeof(HealthStatus), healthStatus.Status));
        }
    }
}
